namespace Mntnr

open Fake.Core
open System.Text.RegularExpressions
open System.IO
open System.Text
open System.Xml

module Toolkit =

  let (|ParseRegex|_|) regex str =
    let m = Regex(regex).Match(str)
    if m.Success
    then Some (List.tail [ for x in m.Groups -> x.Value ])
    else None

  /// Creates a new directory with intermediate path if does not exist
  let mkdir (path:string) = Directory.CreateDirectory path |> ignore

  let private createSh workingDir script =
    RawCommand ("sh", Arguments.OfArgs ["-c"; script])
      |> CreateProcess.fromCommand
      |> CreateProcess.ensureExitCode
      |> CreateProcess.withWorkingDirectory workingDir

  /// Run an arbitrary shell script
  let sh script = 
    let run workingDir script = createSh workingDir script |> Proc.run |> ignore
    run "." script

  /// Run an arbitrary shell script and returns stdout
  let shValue script =
    let run workingDir script = 
      createSh workingDir script
      |> CreateProcess.redirectOutput
      |> CreateProcess.ensureExitCode
      |> Proc.run |> fun x -> x.Result.Output.Trim()
    run "." script

  [<RequireQualifiedAccess>]
  module Git =
    let remoteUrl () =
      shValue "git remote get-url origin"

    let relativePath remoteUrl =
      match remoteUrl with
      | ParseRegex "https://(.+)@([^\/]+)\/(.+).git" [_; _; path] -> path
      | ParseRegex "git@(.+):(.+).git" [_; path] -> path
      | _ -> failwith $"Cannot extract Git path from remote URL: {remoteUrl}"

    let matches pattern (relativePath:string) =
      match relativePath with
      | ParseRegex pattern _ -> true
      | _ -> false

    let remotePathMatches pattern =
      (remoteUrl >> relativePath >> matches pattern) ()

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

  [<RequireQualifiedAccess>]
  module Dockerfile =
    /// Sets a new tag for the given Dockerfile's image
    let setImage imageName newName newTag dockerfile =
      File.stream dockerfile (
        function
         | ParseRegex (sprintf "FROM (%s):([^ ]*)( .*)?" imageName)  [image; _; rest] -> $"FROM %s{match newName with | Some n -> n | None -> image}:%s{newTag}%s{rest}"
         | s -> s)
  
  [<RequireQualifiedAccess>]
  module Xml =
    let append parentXPath nodeXml (xmlFilePath:string) =
      let doc = XmlDocument()
      doc.Load(xmlFilePath)
      let refNode = doc.SelectSingleNode(parentXPath)
      let importDoc = XmlDocument()
      importDoc.LoadXml(nodeXml)
      let nodeToInsert = doc.ImportNode(importDoc.DocumentElement, true)
      refNode.AppendChild nodeToInsert |> ignore
      doc.Save(xmlFilePath)

    let replace xPath content (xmlFilePath:string) =
      let doc = XmlDocument()
      doc.Load(xmlFilePath)
      let node = doc.SelectSingleNode(xPath)
      node.InnerText <- content
      doc.Save(xmlFilePath)

  [<RequireQualifiedAccess>]
  module Csproj =
    let private findProjFileIn csprojDir =
      Directory.EnumerateFiles csprojDir
      |> Seq.tryFind (fun x -> x.EndsWith(".csproj"))
      |> Option.defaultWith (fun () -> failwith $"Directory '{csprojDir}' doesn't contain the csproj file")

    let addProperty xmlProperty csprojDir =
        findProjFileIn csprojDir |> Xml.append "/Project/PropertyGroup[1]" xmlProperty

    let changeTfm tfm csprojDir = 
      findProjFileIn csprojDir |> Xml.replace "/Project/PropertyGroup/TargetFramework" tfm
