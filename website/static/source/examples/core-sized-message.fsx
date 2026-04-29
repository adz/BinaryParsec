#r "../artifacts/api-docs/BinaryParsec/BinaryParsec.dll"

open System
open BinaryParsec
open BinaryParsec.Syntax

type SizedMessage =
    {
        Size: uint32
        Payload: ByteSlice
    }

let message : ContiguousParser<SizedMessage> =
    parse {
        let! size = u32be
        let! payload = takeSlice (int size)

        return
            {
                Size = size
                Payload = payload
            }
    }

let input: byte array =
    [|
        0x00uy; 0x00uy; 0x00uy; 0x05uy
        0x68uy; 0x65uy; 0x6Cuy; 0x6Cuy; 0x6Fuy
    |]

match Contiguous.run message (ReadOnlySpan<byte>(input)) with
| Ok parsed ->
    printfn "Size = %u" parsed.Size
    printfn "Payload offset = %d" parsed.Payload.Offset
    printfn "Payload length = %d" parsed.Payload.Length
    printfn "Payload text = %s" (System.Text.Encoding.ASCII.GetString(ByteSlice.asSpan (ReadOnlySpan<byte>(input)) parsed.Payload))
| Error error ->
    printfn "Error = %s @ %A" error.Message error.Position
