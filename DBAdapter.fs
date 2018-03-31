module DBAdapter

open FSharp.Configuration
type Settings = AppSettings<"App.config">

let connectionString = Settings.ConnectionStrings.ConnectionString

