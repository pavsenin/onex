open System.Net
open System
open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions
open FSharp.Data.HtmlAttribute

let inline (|>>) x f = x |> Option.map f
let inline (|?) def arg = defaultArg arg def
let inline map f d = function | Some v -> f v | None -> d

let rfpl = "Russia-Premier-League"
let champsIds = dict[rfpl, 225733]

let buildLigaUrl id =
    """https://1xstavka.ru/LineFeed/Get1x2_Zip?champs=""" +
    id.ToString() +
    """&count=50&tf=1000000&antisports=38&mode=4&country=1&partner=51"""
let buildMatchUrl id =
    """https://1xstavka.ru/LineFeed/GetGameZip?id=""" +
    id.ToString() +
    """&lng=ru&cfview=0&isSubGames=true&GroupEvents=true&countevents=250&partner=51&grMode=2"""
let getUrl name =
    match champsIds.TryGetValue name with
    | true, id -> Some (buildLigaUrl id)
    | _ -> None
let fetchContent url =
    let req = url |> Uri |> WebRequest.Create
    use resp = req.GetResponse()
    use stream = resp.GetResponseStream()
    use reader = new StreamReader(stream)
    reader.ReadToEnd()
let fromUnixTimestamp secs =
    let origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)
    let time = origin.AddSeconds secs
    let zone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time")
    TimeZoneInfo.ConvertTime(time, zone)
let asF (jsonValue:JsonValue) = jsonValue.AsFloat()

type CoefType =
    | Un
    | P1 | X | P2 
    | D1X | D12 | DX2
    | TG of float
    | TL of float
    | IT1G of float
    | IT1L of float
    | IT2G of float
    | IT2L of float

let toCoefType param _type =
    let toCoef v = map (fun f -> v f) Un
    match _type with
    | 1 -> P1 | 2 -> X | 3 -> P2
    | 4 -> D1X | 5 -> D12 | 6 -> DX2
    | 9 -> toCoef TG param | 10 -> toCoef TL param
    | 11 -> toCoef IT1G param | 12 -> toCoef IT1L param
    | 13 -> toCoef IT2G param | 14 -> toCoef IT2L param
    | _ -> Un

[<EntryPoint>]
let main argv =
    let jsonContent =
        rfpl
        |> getUrl
        |>> fetchContent
        |>> JsonValue.Parse
    match jsonContent with
    | None -> 0
    | Some json ->
        let matches = 
            json?Value.AsArray()
            |> Array.map (fun m ->
                let id = m?CI.AsInteger()
                let name1 = m?O1.AsString()
                let name2 = m?O2.AsString()
                let seconds = m?S.AsFloat()
                let time = fromUnixTimestamp seconds
                let jsonContent =
                    id
                    |> buildMatchUrl
                    |> fetchContent
                    |> JsonValue.Parse
                let coefs =
                    jsonContent?Value?E.AsArray()
                    |> Array.choose (fun k ->
                        let coef = k?C.AsFloat()
                        let _type = k?T.AsInteger()
                        let param = k.TryGetProperty("P") |>> asF
                        let coefType = toCoefType param _type
                        match coefType with
                        | Un -> None
                        | _ -> Some (coefType, coef)
                    )
                (id, name1, name2, time, coefs)
            )
        let filePath = "output.txt"
        use sw = new StreamWriter(path=filePath)
        matches |> Array.iter (fun m -> fprintfn sw "%A" m)
        Console.ReadLine() |> ignore
        0