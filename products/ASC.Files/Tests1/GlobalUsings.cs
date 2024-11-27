global using System.Net;
global using System.Net.Http.Json;
global using System.Text.Json;
global using System.Text.Json.Serialization;

global using ASC.Api.Core.Middleware;
global using ASC.Files.Core.ApiModels.RequestDto;
global using ASC.Files.Core.ApiModels.ResponseDto;
global using ASC.Migrations;
global using ASC.Migrations.Core;

global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using Testcontainers.MySql;
global using Xunit;