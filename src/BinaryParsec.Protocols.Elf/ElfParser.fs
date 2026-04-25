namespace BinaryParsec.Protocols.Elf

open BinaryParsec

[<RequireQualifiedAccess>]
module internal ElfParser =
    let private elfMagic = [| 0x7Fuy; byte 'E'; byte 'L'; byte 'F' |]

    let private elf32ProgramHeaderMinimumSize = 28us
    let private elf64ProgramHeaderMinimumSize = 8us

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
        | _ -> failAt position $"ELF class {rawClass} is not supported by this package."

    let private parseDataEncoding rawData position =
        match rawData with
        | 1uy -> Contiguous.result ElfDataEncoding.LittleEndian
        | 2uy -> Contiguous.result ElfDataEncoding.BigEndian
        | _ -> failAt position $"ELF data encoding {rawData} is not supported by this package."

    let private headerFields elfClass dataEncoding =
        let readUInt16 = uint16ForEncoding dataEncoding
        let readUInt32 = uint32ForEncoding dataEncoding
        let readUInt64 = uint64ForEncoding dataEncoding

        Contiguous.parse {
            do! Contiguous.skip 2
            do! Contiguous.map ignore readUInt16
            do! Contiguous.map ignore readUInt32

            match elfClass with
            | ElfClass.Elf32 ->
                let! entryPoint = readUInt32
                let! programHeaderOffset = readUInt32
                do! Contiguous.skip 4
                do! Contiguous.skip 4
                do! Contiguous.skip 2
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
                do! Contiguous.skip 8
                do! Contiguous.skip 4
                do! Contiguous.skip 2
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
                return! failAt ParsePosition.origin $"ELF class %A{elfClass} is out of range."
        }

    let header =
        Contiguous.parse {
            do! Contiguous.map ignore (Contiguous.expectBytes elfMagic "ELF magic mismatch.")

            let! classPosition = Contiguous.position
            let! rawClass = Contiguous.``byte``
            let! elfClass = parseClass rawClass classPosition

            let! dataPosition = Contiguous.position
            let! rawData = Contiguous.``byte``
            let! dataEncoding = parseDataEncoding rawData dataPosition

            do! Contiguous.skip 10

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
            Contiguous.parse {
                let! headerType = readUInt32
                do! Contiguous.skip 20
                let! flags = readUInt32

                return
                    { Type = headerType
                      Flags = flags }
            }
        | ElfClass.Elf64 ->
            Contiguous.parse {
                let! headerType = readUInt32
                let! flags = readUInt32

                return
                    { Type = headerType
                      Flags = flags }
            }
        | _ ->
            failAt ParsePosition.origin $"ELF class %A{elfClass} is out of range."

    let programHeaderAt (header: ElfFileHeader) (index: int) =
        if index < 0 then
            invalidArg (nameof index) "Program-header index must be non-negative."

        Contiguous.parse {
            let! currentPosition = Contiguous.position

            if int header.ProgramHeaderEntryCount <= index then
                return!
                    failAt
                        currentPosition
                        $"ELF program-header index {index} is outside the available table entry count {header.ProgramHeaderEntryCount}."
            elif header.ProgramHeaderEntrySize < programHeaderMinimumSize header.Class then
                return!
                    failAt
                        currentPosition
                        $"ELF program-header entry size {header.ProgramHeaderEntrySize} is too small for %A{header.Class}."
            else
                let entryOffset =
                    header.ProgramHeaderOffset + (uint64 index * uint64 header.ProgramHeaderEntrySize)

                if entryOffset > uint64 System.Int32.MaxValue then
                    return!
                        failAt
                            currentPosition
                            "ELF program-header offset exceeds supported contiguous input size."
                else
                    return!
                        Contiguous.readAt (int entryOffset) (rawProgramHeader header.Class header.DataEncoding)
        }

    let file =
        Contiguous.parse {
            let! parsedHeader = header
            let! currentPosition = Contiguous.position

            if parsedHeader.ProgramHeaderEntryCount = 0us then
                return! failAt currentPosition "ELF file does not contain a program header table."
            else
                let! firstProgramHeader = programHeaderAt parsedHeader 0

                return
                    { Header = parsedHeader
                      FirstProgramHeader = firstProgramHeader }
        }
