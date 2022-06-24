namespace Mntnr.Toolkit

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
    File.AppendAllLines(filePath, "\n"::lines |> Seq.toArray, utf8NoBom) 

  /// Reads file content
  let readLines (path:string) = File.ReadAllLines path |> Seq.ofArray

  let readContent (path:string) =
    Path.Combine(Environment.environVar "MNTNR_CLONE_DIR", path) |> readLines

  let private apply map filePath (lines:string seq) = 
    lines |> map |> writeLines filePath

  let private applyInline map filePath =
    filePath |> readLines |> apply map filePath

  /// Scans file line-by-line enabling basic replacements
  let stream filePath map = filePath |> applyInline (Seq.map map)

  /// Streams lines one-by-one and outputs to a file
  let streamTo destFilePath map lines = lines |> apply (Seq.map map) destFilePath

  /// Extracts arbitrary variables from the original file and replaces the file content with the new one constructed with makeResult function 
  let replace (filePath:string) (bindVars: string seq ->'a) (makeResult: 'a -> string list) =
    filePath |> applyInline (bindVars >> makeResult)

  /// Extracts arbitrary variables from the input lines and sets the destination file content with the new one constructed with makeResult function 
  let replaceTo (destFilePath:string) (bindVars: string seq ->'a) (makeResult: 'a -> string list) lines =
    lines |> apply (bindVars >> makeResult) destFilePath

  /// Insert lines created by linesFunc after each line marked by markerLineRegexp.
  /// linesFunc takes markerLineRegexp captures as parameters so they can be used in
  /// creation of the output lines.
  let insertAfter (markerLineRegexp:string) (linesFunc: string list -> string list) =
      function
      | ParseRegex markerLineRegexp captures as matched -> $"{matched}\n" + (linesFunc captures |> String.concat "\n")
      | s -> s
  
  /// Inserts a single line after each line matched by markerLineRegexp
  let insertLineAfter (markerLineRegexp:string) (lineToInsert: string) =
    insertAfter markerLineRegexp (fun _ -> [ lineToInsert ])

  /// Creates a new file or overwrites an existing one
  let create = writeLines

  /// Deletes file
  let delete path = File.Delete path
