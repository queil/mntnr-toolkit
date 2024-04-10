namespace Mntnr.Toolkit

open System.Xml

[<RequireQualifiedAccess>]
module Xml =

    open System.IO

    let private ensureFinalEol file =
        use stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite)
        stream.Seek(-1, SeekOrigin.End) |> ignore

        if stream.ReadByte() <> 10 then
            stream.WriteByte(10uy)

        stream.Flush()

    type XmlDocument with
        member x.SaveWithFinalEol(path: string) =
            x.Save(path)
            ensureFinalEol path

    let appendNode parentXPath nodeXml (xmlFilePath: string) =
        let doc = XmlDocument()
        doc.PreserveWhitespace <- true
        doc.Load(xmlFilePath)
        let refNode = doc.SelectSingleNode(parentXPath)

        let (preWhitespace, postWhitespace) =
            let maybeWhitespace = refNode.LastChild :?> XmlWhitespace

            if isNull maybeWhitespace then
                let spaces = (refNode.PreviousSibling :?> XmlWhitespace).InnerText.Trim('\r', '\n')
                ("\n" + spaces + spaces, "\n" + spaces)
            else
                let spaces = maybeWhitespace.InnerText.TrimStart('\r', '\n')

                if spaces = "" then
                    let maybeFutureSiblingIndent =
                        match refNode.LastChild with
                        | null -> ""
                        | n ->
                            match n.PreviousSibling with
                            | null -> ""
                            | n -> (n.LastChild :?> XmlWhitespace).InnerText

                    let indent = maybeFutureSiblingIndent
                    (indent.TrimStart('\r', '\n'), "\n" + indent.Trim(' '))
                else
                    (spaces, "\n" + spaces)

        let importDoc = XmlDocument()
        importDoc.LoadXml(nodeXml)
        let nodeToInsert = doc.ImportNode(importDoc.DocumentElement, true)
        refNode.AppendChild(doc.CreateWhitespace(preWhitespace)) |> ignore
        refNode.AppendChild nodeToInsert |> ignore
        refNode.AppendChild(doc.CreateWhitespace(postWhitespace)) |> ignore
        doc.SaveWithFinalEol(xmlFilePath)

    let replaceNodeText xPath mapContent (xmlFilePath: string) =
        let doc = XmlDocument()
        doc.PreserveWhitespace <- true
        doc.Load(xmlFilePath)
        let node = doc.SelectSingleNode(xPath)
        node.InnerText <- mapContent node.InnerText
        doc.SaveWithFinalEol(xmlFilePath)

    let removeNode xPath (xmlFilePath: string) =
        let doc = XmlDocument()
        doc.PreserveWhitespace <- true
        doc.Load(xmlFilePath)
        let node = doc.SelectSingleNode(xPath)

        if isNull node then
            ()
        else
            let maybe_whitespace = node.PreviousSibling :?> XmlWhitespace

            if not <| isNull maybe_whitespace then
                node.ParentNode.RemoveChild(maybe_whitespace) |> ignore

            node.ParentNode.RemoveChild(node) |> ignore
            doc.SaveWithFinalEol(xmlFilePath)
