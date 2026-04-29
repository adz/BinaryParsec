#r "../artifacts/api-docs/BinaryParsec/BinaryParsec.dll"
#r "../artifacts/api-docs/BinaryParsec.Protocols.Png/BinaryParsec.Protocols.Png.dll"

open System
open BinaryParsec
open BinaryParsec.Protocols.Png

let input: byte array =
    [|
        0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy
        0x00uy; 0x00uy; 0x00uy; 0x0Duy
        0x49uy; 0x48uy; 0x44uy; 0x52uy
        0x00uy; 0x00uy; 0x00uy; 0x01uy
        0x00uy; 0x00uy; 0x00uy; 0x01uy
        0x08uy; 0x02uy; 0x00uy; 0x00uy; 0x00uy
        0x90uy; 0x77uy; 0x53uy; 0xDEuy
    |]

match Contiguous.run Png.initialSlice (ReadOnlySpan<byte>(input)) with
| Ok parsed ->
    printfn "Signature length = %d" parsed.Signature.Length
    printfn "First chunk type = %s" (System.Text.Encoding.ASCII.GetString(ByteSlice.asSpan (ReadOnlySpan<byte>(input)) parsed.FirstChunk.ChunkType))
    printfn "First chunk payload length = %d" parsed.FirstChunk.Payload.Length
| Error error ->
    printfn "Error = %s @ %A" error.Message error.Position
