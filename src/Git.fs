namespace Mntnr.Toolkit

[<RequireQualifiedAccess>]
module Git =
    let remoteUrl () = shValue "git remote get-url origin"

    let relativePath remoteUrl =
        match remoteUrl with
        | ParseRegex "https:\/\/(.+)@([^\/]+)\/(.+).git" [ _; _; path ] -> path
        | ParseRegex "https:\/\/([^\/]+)\/(.+)" [ _; path ] -> path
        | ParseRegex "git@(.+):(.+).git" [ _; path ] -> path
        | ParseRegex "git@(.+):(.+)" [ _; path ] -> path
        | _ -> failwith $"Cannot extract Git path from remote URL: {remoteUrl}"

    let matches pattern (relativePath: string) =
        match relativePath with
        | ParseRegex pattern _ -> true
        | _ -> false

    let remotePathMatches pattern =
        (remoteUrl >> relativePath >> matches pattern) ()
