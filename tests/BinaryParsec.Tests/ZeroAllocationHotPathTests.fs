module BinaryParsec.Tests.ZeroAllocationHotPathTests

open System
open BinaryParsec
open BinaryParsec.Protocols.Modbus
open BinaryParsec.Protocols.Png
open Swensen.Unquote
open Xunit

let private assertZeroAllocations (parser: ContiguousParser<'T>) (bytes: byte array) =
    let input = ReadOnlySpan<byte>(bytes)

    for _ = 1 to 10_000 do
        Contiguous.run parser input |> ignore

    GC.Collect()
    GC.WaitForPendingFinalizers()
    GC.Collect()

    let before = GC.GetAllocatedBytesForCurrentThread()

    for _ = 1 to 100_000 do
        Contiguous.run parser input |> ignore

    let allocated = GC.GetAllocatedBytesForCurrentThread() - before
    test <@ allocated = 0L @>

[<Fact>]
let ``primitive byte read stays allocation free`` () =
    assertZeroAllocations Contiguous.``byte`` [| 0x2Auy; 0x7Fuy |]

[<Fact>]
let ``zero copy slice read stays allocation free`` () =
    assertZeroAllocations (Contiguous.take 3) [| 0x10uy; 0x20uy; 0x30uy; 0x40uy |]

[<Fact>]
let ``png initial slice stays allocation free`` () =
    assertZeroAllocations
        Png.initialSlice
        [|
            0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
            0x00uy; 0x00uy; 0x00uy; 0x0Duy
            0x49uy; 0x48uy; 0x44uy; 0x52uy
            0x00uy; 0x00uy; 0x00uy; 0x01uy
            0x00uy; 0x00uy; 0x00uy; 0x01uy
            0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy
            0x90uy; 0x77uy; 0x53uy; 0xDEuy
        |]

[<Fact>]
let ``modbus frame parse stays allocation free`` () =
    assertZeroAllocations
        ModbusRtu.frame
        [|
            0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0xCDuy
        |]
