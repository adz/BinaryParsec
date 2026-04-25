namespace BinaryParsec.Protocols.Elf

open BinaryParsec
open BinaryParsec.Syntax

[<RequireQualifiedAccess>]
module internal ElfParser =
    let private elfMagic =
        [|
            0x7Fuy
            uint8 'E'
            uint8 'L'
            uint8 'F'
        |]

    let private elf32ProgramHeaderMinimumSize = 28us
    let private elf64ProgramHeaderMinimumSize = 8us

    let private uint16ForEncoding dataEncoding =
        match dataEncoding with
        | ElfDataEncoding.LittleEndian -> u16le
        | ElfDataEncoding.BigEndian -> u16be
        | _ -> invalidOp "Unsupported ELF data encodings are rejected before width reads."

    let private uint32ForEncoding dataEncoding =
        match dataEncoding with
        | ElfDataEncoding.LittleEndian -> u32le
        | ElfDataEncoding.BigEndian -> u32be
        | _ -> invalidOp "Unsupported ELF data encodings are rejected before width reads."

    let private uint64ForEncoding dataEncoding =
        match dataEncoding with
        | ElfDataEncoding.LittleEndian -> u64le
        | ElfDataEncoding.BigEndian -> u64be
        | _ -> invalidOp "Unsupported ELF data encodings are rejected before width reads."

    let private parseClass rawClass position =
        match rawClass with
        | 1uy -> result ElfClass.Elf32
        | 2uy -> result ElfClass.Elf64
        | _ -> fail position $"ELF class {rawClass} is not supported by this package."

    let private parseDataEncoding rawData position =
        match rawData with
        | 1uy -> result ElfDataEncoding.LittleEndian
        | 2uy -> result ElfDataEncoding.BigEndian
        | _ -> fail position $"ELF data encoding {rawData} is not supported by this package."

    let private headerFields elfClass dataEncoding =
        let readUInt16 = uint16ForEncoding dataEncoding
        let readUInt32 = uint32ForEncoding dataEncoding
        let readUInt64 = uint64ForEncoding dataEncoding

        parse {
            do! skip 2
            do! map ignore readUInt16
            do! map ignore readUInt32

            match elfClass with
            | ElfClass.Elf32 ->
                let! entryPoint = readUInt32
                let! programHeaderOffset = readUInt32
                do! skip 4
                do! skip 4
                do! skip 2
                let! programHeaderEntrySize = readUInt16
                let! programHeaderEntryCount = readUInt16

                return
                    { Class = elfClass
                      DataEncoding = dataEncoding
                      EntryPoint = uint64 entryPoint
                      ProgramHeaderOffset = uint64 programHeaderOffset
                      ProgramHeaderEntrySize = programHeaderEntrySize
                      ProgramHeaderEntryCount = programHeaderEntryCount }
            | ElfClass.Elf64 ->
                let! entryPoint = readUInt64
                let! programHeaderOffset = readUInt64
                do! skip 8
                do! skip 4
                do! skip 2
                let! programHeaderEntrySize = readUInt16
                let! programHeaderEntryCount = readUInt16

                return
                    { Class = elfClass
                      DataEncoding = dataEncoding
                      EntryPoint = entryPoint
                      ProgramHeaderOffset = programHeaderOffset
                      ProgramHeaderEntrySize = programHeaderEntrySize
                      ProgramHeaderEntryCount = programHeaderEntryCount }
            | _ ->
                return! fail ParsePosition.origin $"ELF class %A{elfClass} is out of range."
        }

    let header =
        parse {
            do! map ignore (expectBytes elfMagic "ELF magic mismatch.")

            let! classPosition = position
            let! rawClass = ``byte``
            let! elfClass = parseClass rawClass classPosition

            let! dataPosition = position
            let! rawData = ``byte``
            let! dataEncoding = parseDataEncoding rawData dataPosition

            do! skip 10

            return! headerFields elfClass dataEncoding
        }

    let private programHeaderMinimumSize elfClass =
        match elfClass with
        | ElfClass.Elf32 -> elf32ProgramHeaderMinimumSize
        | ElfClass.Elf64 -> elf64ProgramHeaderMinimumSize
        | _ -> invalidOp "Unsupported ELF classes are rejected before program-header lookup."

    let private rawProgramHeader elfClass dataEncoding =
        let readUInt32 = uint32ForEncoding dataEncoding

        match elfClass with
        | ElfClass.Elf32 ->
            parse {
                let! headerType = readUInt32
                do! skip 20
                let! flags = readUInt32

                return
                    { Type = headerType
                      Flags = flags }
            }
        | ElfClass.Elf64 ->
            parse {
                let! headerType = readUInt32
                let! flags = readUInt32

                return
                    { Type = headerType
                      Flags = flags }
            }
        | _ ->
            fail ParsePosition.origin $"ELF class %A{elfClass} is out of range."

    let programHeaderAt (header: ElfFileHeader) (index: int) =
        if index < 0 then
            invalidArg (nameof index) "Program-header index must be non-negative."

        parse {
            let! currentPosition = position

            if int header.ProgramHeaderEntryCount <= index then
                return!
                    fail
                        currentPosition
                        $"ELF program-header index {index} is outside the available table entry count {header.ProgramHeaderEntryCount}."
            elif header.ProgramHeaderEntrySize < programHeaderMinimumSize header.Class then
                return!
                    fail
                        currentPosition
                        $"ELF program-header entry size {header.ProgramHeaderEntrySize} is too small for %A{header.Class}."
            else
                let entryOffset =
                    header.ProgramHeaderOffset + (uint64 index * uint64 header.ProgramHeaderEntrySize)

                if entryOffset > uint64 System.Int32.MaxValue then
                    return!
                        fail
                            currentPosition
                            "ELF program-header offset exceeds supported contiguous input size."
                else
                    return! readAt (int entryOffset) (rawProgramHeader header.Class header.DataEncoding)
        }

    let file =
        parse {
            let! parsedHeader = header
            let! currentPosition = position

            if parsedHeader.ProgramHeaderEntryCount = 0us then
                return! fail currentPosition "ELF file does not contain a program header table."
            else
                let! firstProgramHeader = programHeaderAt parsedHeader 0

                return
                    { Header = parsedHeader
                      FirstProgramHeader = firstProgramHeader }
        }
