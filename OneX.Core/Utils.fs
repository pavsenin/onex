namespace OneX.Core
open System
open System.Net
open System.IO
open FSharp.Data
open FSharp.Data.JsonExtensions

module Utils =

    type Result<'TSuccess, 'TFailure> = Success of 'TSuccess | Failure of 'TFailure
    let inline defArgr d v = match v with | Success s -> s | _ -> d

    let inline (|>>) x f = x |> Option.map f
    let inline (||>) x f = x |> Option.bind f
    let inline (|?) def arg = defaultArg arg def
    let inline map f d = function | Some v -> f v | None -> d
    let inline mapl f = function | [] -> [] | l -> f l 
    let inline defArg d v = defaultArg v d
    let inline cond v t f = if v then t else f
    let inline split (del:char []) (opts:StringSplitOptions) (s:String) = s.Split(del, opts)
    let inline splitNoEmpty (del:char []) = split del StringSplitOptions.RemoveEmptyEntries
    let inline isEmpty (str:string) = String.IsNullOrEmpty str

module WebUtils =

    let inline asF (jsonValue:JsonValue) = jsonValue.AsFloat()

    let fetchContent adustRequest url =
        let req = url |> Uri |> WebRequest.Create
        adustRequest req
        use resp = req.GetResponse()
        use stream = resp.GetResponseStream()
        use reader = new StreamReader(stream)
        reader.ReadToEnd()

    let fetchContentGet = fetchContent (fun _ -> ())

module BenchmarkUtils =

    let inline showTime title func =
        let watch = System.Diagnostics.Stopwatch()
        watch.Start()
        let result = func()
        watch.Stop()
        Console.WriteLine(title + ": " + watch.ElapsedMilliseconds.ToString())
        result