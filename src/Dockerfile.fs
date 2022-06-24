namespace Mntnr.Toolkit

[<RequireQualifiedAccess>]
module Dockerfile =
  /// Sets a new tag for the given Dockerfile's image
  let setImage imageName newName newTag dockerfilePath =
    File.stream dockerfilePath (
      function
       | ParseRegex (sprintf "FROM (%s):([^ ]*)( .*)?" imageName)  [image; _; rest] -> $"FROM %s{match newName with | Some n -> n | None -> image}:%s{newTag}%s{rest}"
       | s -> s)
