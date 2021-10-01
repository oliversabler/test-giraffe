﻿open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Todos
open System
open Microsoft.Extensions.Logging

/////////////
// Web App //
/////////////

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

let apiTodoRoutes : HttpHandler =
    subRoute "/todo/"
        (choose [
            GET >=> choose [
                routef "%O" Handlers.getTaskHandler
                route "" >=> Handlers.getTasksHandler
            ]
            POST >=> route "" >=> Handlers.createTaskHandler
            PUT >=> route "" >=> Handlers.updateTaskHandler
            DELETE >=> routef "%O" Handlers.deleteTaskHandler
        ])

let webApp =
    choose [
        route "/ping"   >=> text "pong"
        GET >=> route "/"
        subRoute "/api"
            (choose [
                apiTodoRoutes
            ])
        setStatusCode 404 >=> text "Not Found"
    ]

///////////////////
// Configuration //
///////////////////

let configureApp (app : IApplicationBuilder) =
    app.UseGiraffeErrorHandler(errorHandler)
       .UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddGiraffe()
            .AddSingleton<Store>(Store()) |> ignore

let configureLogging (loggingBuilder : ILoggingBuilder) =
    loggingBuilder.AddFilter(fun lvl -> lvl.Equals LogLevel.Error)
                  .AddConsole()
                  .AddDebug() |> ignore

[<EntryPoint>]
let main _ =
    Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(fun webHost ->
            webHost
                .Configure(configureApp)
                .ConfigureServices(configureServices)
                .ConfigureLogging(configureLogging)
                |> ignore)
        .Build()
        .Run()
    0