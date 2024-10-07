namespace Mntnr.Toolkit

#r "paket:
      nuget Fake.Core.Environment = 6.1.1
"
#load "Common.fsx"

open System.IO
open System.Text
open Fake.Core

[<RequireQualifiedAccess>]
module File =
    let private utf8NoBom = UTF8Encoding(false)

    let private writeLines filePath lines =
        File.WriteAllLines(filePath, lines |> Seq.toArray, utf8NoBom)

    /// Appends lines to a file
    let appendLines filePath lines =
        File.AppendAllLines(filePath, lines |> Seq.toArray, utf8NoBom)

    /// Reads all lines from a file
    let readLines (path: string) = File.ReadAllLines path |> Seq.ofArray

    /// Reads all lines from a file located in the Mntnr content dir
    let readFromContentDir (path: string) =
        Path.Combine(Environment.environVar "MNTNR_CLONE_DIR", path) |> readLines

    let private apply (map: string seq -> string option seq) filePath (lines: string seq) = lines |> map |> Seq.choose id |> writeLines filePath

    let private applyInline map filePath =
        filePath |> readLines |> apply map filePath

    /// Scans file line-by-line enabling basic replacements
    let stream filePath (map:string -> string) = filePath |> applyInline (Seq.map map >> Seq.map Some)

    /// Scans file line-by-line enabling basic replacements
    let streamOptional filePath (map:string -> string option) = filePath |> applyInline (Seq.map map)

    /// Scans file line-by-line enabling basic replacements
    let streamIndexed filePath (map: int -> string -> string) = filePath |> applyInline (Seq.mapi map >> Seq.map Some) 

    /// Streams lines one-by-one and outputs to a file
    let streamTo destFilePath map lines =
        lines |> apply (Seq.map map) destFilePath

    /// Replaces the whole file content extracting variables from the original file
    let replaceContent (filePath: string) (bindVars: string seq -> 'a) (makeNewFileContent: 'a -> string list) =
        filePath |> applyInline (bindVars >> (makeNewFileContent >> Seq.map Some)) 

    /// Extracts arbitrary variables from the input lines and sets the destination file content with the new one constructed with makeResult function
    let replaceTo (destFilePath: string) (bindVars: string seq -> 'a) (makeResult: 'a -> string list) lines =
        lines |> apply (bindVars >> makeResult >> Seq.map Some) destFilePath

    /// Insert lines created by linesFunc after each line marked by markerLineRegexp.
    /// linesFunc takes markerLineRegexp captures as parameters so they can be used in
    /// creation of the output lines.
    let insertAfter (markerLineRegexp: string) (linesFunc: string list -> string list) =
        function
        | ParseRegex markerLineRegexp captures as matched -> $"{matched}\n" + (linesFunc captures |> String.concat "\n")
        | s -> s

    /// Inserts a single line after each line matched by markerLineRegexp
    let insertLineAfter (markerLineRegexp: string) (lineToInsert: string) =
        insertAfter markerLineRegexp (fun _ -> [ lineToInsert ])

    /// Insert lines provided at a particular line number. 0 = prepend
    let insertAt (index: int) (lines: string list) =
        fun idx (line: string) ->
            match (idx, line) with
            | (idx, line) when idx = index -> [ yield! lines; line ] |> String.concat "\n"
            | (_, line) -> line

    let prependLines (filePath: string) (lines: string list) =
        lines |> insertAt 0 |> streamIndexed filePath

    /// Creates a new file or overwrites an existing one
    let create = writeLines

    /// Deletes file
    let delete path = File.Delete path
