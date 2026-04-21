#load "../src/BinaryParsec/BinaryParsec.fs"

open System
open BinaryParsec

let inline private fail message =
    raise (InvalidOperationException(message))

let private assertZeroAllocations name (parser: ContiguousParser<'T>) (bytes: byte array) =
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

    if allocated <> 0L then
        fail $"%s{name}: expected zero allocations on the successful hot path, got %d{allocated} bytes."

let primitiveByteReadStaysAllocationFree () =
    assertZeroAllocations "byte hot path" Contiguous.``byte`` [| 0x2Auy; 0x7Fuy |]

let zeroCopySliceReadStaysAllocationFree () =
    assertZeroAllocations "take hot path" (Contiguous.take 3) [| 0x10uy; 0x20uy; 0x30uy; 0x40uy |]

let pngInitialSliceStaysAllocationFree () =
    assertZeroAllocations
        "png initial slice hot path"
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

let modbusFrameParseStaysAllocationFree () =
    assertZeroAllocations
        "modbus frame hot path"
        ModbusRtu.frame
        [|
            0x01uy; 0x03uy; 0x00uy; 0x00uy; 0x00uy; 0x0Auy; 0xC5uy; 0xCDuy
        |]

let tests =
    [ primitiveByteReadStaysAllocationFree
      zeroCopySliceReadStaysAllocationFree
      pngInitialSliceStaysAllocationFree
      modbusFrameParseStaysAllocationFree ]

for test in tests do
    test ()
