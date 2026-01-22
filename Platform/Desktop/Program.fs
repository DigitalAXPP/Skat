namespace Skat.Desktop

open System
open Avalonia
open Fabulous.Avalonia
open Skat
open SignalRClient

module Program =

    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp () =
        
        //let hubService = HubService(
        //    "http://localhost:5109/gamehub",
        //    fun serverMsg ->
        //        Program.dispatch (AppMsg.ServerMsg serverMsg)
        //)
        
        AppBuilder
            .Configure(fun () ->
                let app = Program.startApplication App.program //hubService)
                app.Styles.Add(App.theme)
                app)
            .LogToTrace(areas = Array.empty)
            .UsePlatformDetect()

    [<EntryPoint; STAThread>]
    let main argv =
        buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
