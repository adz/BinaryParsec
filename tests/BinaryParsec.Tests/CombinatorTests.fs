namespace BinaryParsec.Tests

open System
open BinaryParsec
open BinaryParsec.Syntax
open Swensen.Unquote
open Xunit

[<RequireQualifiedAccess>]
module CombinatorTests =
    let private expectSuccess expectedValue expectedPosition result =
        match result with
        | Ok(struct (value, position)) ->
            test <@ value = expectedValue @>
            test <@ position = expectedPosition @>
        | Error error ->
            raise (Xunit.Sdk.XunitException($"Expected success, got %A{error}"))

    let private expectFailure expectedPosition expectedMessage result =
        match result with
        | Ok(struct (value, position)) ->
            raise (Xunit.Sdk.XunitException($"Expected failure, got value %A{value} at %A{position}"))
        | Error error ->
            test <@ error.Position = expectedPosition @>
            test <@ error.Message = expectedMessage @>

    let private invoke (parser: ContiguousParser<'T>) (bytes: byte array) position =
        parser.Invoke(ReadOnlySpan<byte>(bytes), position)

    [<Fact>]
    let ``choice picks the first successful parser`` () =
        let parser = choice [ fail ParsePosition.origin "no"; result 42; result 99 ]
        invoke parser [||] ParsePosition.origin
        |> expectSuccess 42 ParsePosition.origin

    [<Fact>]
    let ``choice fails if all parsers fail`` () =
        let parser = choice [ fail ParsePosition.origin "fail1"; fail ParsePosition.origin "fail2" ]
        invoke parser [||] ParsePosition.origin
        |> expectFailure ParsePosition.origin "fail2"

    [<Fact>]
    let ``optional returns Some on success and None on failure without moving cursor`` () =
        let parser = optional ``byte``
        invoke parser [| 0x01uy |] ParsePosition.origin
        |> expectSuccess (Some 0x01uy) (ParsePosition.create 1 0)

        invoke parser [||] ParsePosition.origin
        |> expectSuccess None ParsePosition.origin

    [<Fact>]
    let ``many runs zero or more times`` () =
        let parser = many ``byte``
        invoke parser [| 0x01uy; 0x02uy |] ParsePosition.origin
        |> expectSuccess [0x01uy; 0x02uy] (ParsePosition.create 2 0)

        invoke parser [||] ParsePosition.origin
        |> expectSuccess [] ParsePosition.origin

    [<Fact>]
    let ``many1 runs one or more times`` () =
        let parser = many1 ``byte``
        invoke parser [| 0x01uy |] ParsePosition.origin
        |> expectSuccess [0x01uy] (ParsePosition.create 1 0)

        invoke parser [||] ParsePosition.origin
        |> expectFailure ParsePosition.origin "Unexpected end of input while reading 1 byte(s)."

    [<Fact>]
    let ``sepBy parses items with separators`` () =
        let parser = sepBy ``byte`` (expectBytes [| 0x00uy |] "sep")
        invoke parser [| 0x01uy; 0x00uy; 0x02uy |] ParsePosition.origin
        |> expectSuccess [0x01uy; 0x02uy] (ParsePosition.create 3 0)

        invoke parser [| 0x01uy |] ParsePosition.origin
        |> expectSuccess [0x01uy] (ParsePosition.create 1 0)

        invoke parser [||] ParsePosition.origin
        |> expectSuccess [] ParsePosition.origin

    [<Fact>]
    let ``sepBy1 requires at least one item`` () =
        let parser = sepBy1 ``byte`` (expectBytes [| 0x00uy |] "sep")
        invoke parser [| 0x01uy |] ParsePosition.origin
        |> expectSuccess [0x01uy] (ParsePosition.create 1 0)

        invoke parser [||] ParsePosition.origin
        |> expectFailure ParsePosition.origin "Unexpected end of input while reading 1 byte(s)."

    [<Fact>]
    let ``between parses surrounded item`` () =
        let parser = between (expectBytes [| 0x01uy |] "open") (expectBytes [| 0x02uy |] "close") ``byte``
        invoke parser [| 0x01uy; 0xAAuy; 0x02uy |] ParsePosition.origin
        |> expectSuccess 0xAAuy (ParsePosition.create 3 0)

    [<Fact>]
    let ``label prefixes error message`` () =
        let parser = label "MyLabel" (fail ParsePosition.origin "Original")
        invoke parser [||] ParsePosition.origin
        |> expectFailure ParsePosition.origin "MyLabel: Original"

    [<Fact>]
    let ``operators provide idiomatic F# syntax`` () =
        let parser = ``byte`` .>>. u16be |>> fun (b, u) -> (int b, int u)
        invoke parser [| 0x01uy; 0x00uy; 0x02uy |] ParsePosition.origin
        |> expectSuccess (1, 2) (ParsePosition.create 3 0)

        let choiceParser = result 1 <|> result 2
        invoke choiceParser [||] ParsePosition.origin
        |> expectSuccess 1 ParsePosition.origin

        let labelParser = fail ParsePosition.origin "fail" <?> "labeled"
        invoke labelParser [||] ParsePosition.origin
        |> expectFailure ParsePosition.origin "labeled: fail"

    [<Fact>]
    let ``requireByteAligned enforces byte boundary`` () =
        let parser = bits 4 >>. requireByteAligned >>. bits 4
        invoke parser [| 0xFFuy |] ParsePosition.origin
        |> expectFailure (ParsePosition.create 0 4) "Byte-aligned primitive cannot run when the cursor is at a bit offset."
