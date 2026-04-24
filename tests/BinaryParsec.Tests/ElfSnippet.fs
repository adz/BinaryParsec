namespace BinaryParsec.Tests

open System
open BinaryParsec

/// Identifies the two ELF file classes that this snippet handles.
[<RequireQualifiedAccess>]
type ElfClass =
    | Elf32 = 1
    | Elf64 = 2

/// Identifies the ELF byte-order encodings that this snippet handles.
[<RequireQualifiedAccess>]
type ElfDataEncoding =
    | LittleEndian = 1
    | BigEndian = 2

/// Captures one tiny ELF header plus the leading fields of the first program-header entry.
type ElfSnippet =
    {
        Class: ElfClass
        DataEncoding: ElfDataEncoding
        EntryPoint: uint64
        ProgramHeaderOffset: uint64
        FirstProgramHeaderType: uint32
        FirstProgramHeaderFlags: uint32
    }

[<RequireQualifiedAccess>]
module internal ElfSnippet =
    let private elfMagic = [| 0x7Fuy; byte 'E'; byte 'L'; byte 'F' |]

    let private failAt position message =
        ContiguousParser<_>(fun _ _ -> Contiguous.failAt position message)

    let private uint16ForEncoding dataEncoding =
        match dataEncoding with
        | ElfDataEncoding.LittleEndian -> Contiguous.u16le
        | ElfDataEncoding.BigEndian -> Contiguous.u16be
        | _ -> invalidOp "Unsupported ELF data encodings are rejected before width reads."

    let private uint32ForEncoding dataEncoding =
        match dataEncoding with
        | ElfDataEncoding.LittleEndian -> Contiguous.u32le
        | ElfDataEncoding.BigEndian -> Contiguous.u32be
        | _ -> invalidOp "Unsupported ELF data encodings are rejected before width reads."

    let private uint64ForEncoding dataEncoding =
        match dataEncoding with
        | ElfDataEncoding.LittleEndian -> Contiguous.u64le
        | ElfDataEncoding.BigEndian -> Contiguous.u64be
        | _ -> invalidOp "Unsupported ELF data encodings are rejected before width reads."

    let private parseClass rawClass position =
        match rawClass with
        | 1uy -> Contiguous.result ElfClass.Elf32
        | 2uy -> Contiguous.result ElfClass.Elf64
        | _ -> failAt position $"ELF class {rawClass} is not supported by this snippet."

    let private parseDataEncoding rawData position =
        match rawData with
        | 1uy -> Contiguous.result ElfDataEncoding.LittleEndian
        | 2uy -> Contiguous.result ElfDataEncoding.BigEndian
        | _ -> failAt position $"ELF data encoding {rawData} is not supported by this snippet."

    let private headerFields elfClass dataEncoding =
        let readUInt32 = uint32ForEncoding dataEncoding
        let readUInt64 = uint64ForEncoding dataEncoding

        Contiguous.parse {
            do! Contiguous.skip 2
            do! Contiguous.map ignore (uint16ForEncoding dataEncoding)
            do! Contiguous.map ignore readUInt32

            match elfClass with
            | ElfClass.Elf32 ->
                let! entryPoint = readUInt32
                let! programHeaderOffsetPosition = Contiguous.position
                let! programHeaderOffset = readUInt32
                return struct (uint64 entryPoint, programHeaderOffsetPosition, uint64 programHeaderOffset)
            | ElfClass.Elf64 ->
                let! entryPoint = readUInt64
                let! programHeaderOffsetPosition = Contiguous.position
                let! programHeaderOffset = readUInt64
                return struct (entryPoint, programHeaderOffsetPosition, programHeaderOffset)
            | _ ->
                return! failAt ParsePosition.origin $"ELF class %A{elfClass} is out of range."
        }

    let private firstProgramHeader elfClass dataEncoding =
        let readUInt32 = uint32ForEncoding dataEncoding

        match elfClass with
        | ElfClass.Elf32 ->
            Contiguous.parse {
                let! headerType = readUInt32
                do! Contiguous.skip 20
                let! flags = readUInt32
                return headerType, flags
            }
        | ElfClass.Elf64 ->
            Contiguous.parse {
                let! headerType = readUInt32
                let! flags = readUInt32
                return headerType, flags
            }
        | _ ->
            failAt ParsePosition.origin $"ELF class %A{elfClass} is out of range."

    /// Parses the ELF identification bytes, the entry-point and program-header offset fields,
    /// then follows that offset to the first program-header entry.
    ///
    /// The layout follows the System V ABI ELF header and program-header table definitions.
    let file =
        Contiguous.parse {
            do! Contiguous.map ignore (Contiguous.expectBytes elfMagic "ELF magic mismatch.")

            let! classPosition = Contiguous.position
            let! rawClass = Contiguous.``byte``
            let! elfClass = parseClass rawClass classPosition

            let! dataPosition = Contiguous.position
            let! rawData = Contiguous.``byte``
            let! dataEncoding = parseDataEncoding rawData dataPosition

            do! Contiguous.skip 10

            let! struct (entryPoint, programHeaderOffsetPosition, programHeaderOffset) = headerFields elfClass dataEncoding

            if programHeaderOffset > uint64 Int32.MaxValue then
                return! failAt programHeaderOffsetPosition "ELF program-header offset exceeds supported contiguous input size."
            else
                let! struct (firstProgramHeaderType, firstProgramHeaderFlags) =
                    Contiguous.readAt (int programHeaderOffset) (firstProgramHeader elfClass dataEncoding)
                    |> Contiguous.map (fun (headerType, flags) -> struct (headerType, flags))

                return
                    {
                        Class = elfClass
                        DataEncoding = dataEncoding
                        EntryPoint = entryPoint
                        ProgramHeaderOffset = programHeaderOffset
                        FirstProgramHeaderType = firstProgramHeaderType
                        FirstProgramHeaderFlags = firstProgramHeaderFlags
                    }
        }
