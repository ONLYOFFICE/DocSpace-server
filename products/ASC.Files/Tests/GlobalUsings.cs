extern alias ASCWebApi;
extern alias ASCFiles;
extern alias ASCPeople;
extern alias ASCFilesService;
global using System.Data.Common;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Security.Cryptography;
global using System.Text;
global using System.Text.Json;
global using System.Web;

global using ASC.Core.Common.EF;
global using ASC.Files.Core.ApiModels.ResponseDto;
global using ASC.Files.Tests.Data;
global using ASC.Migrations;
global using ASC.Migrations.Core;

global using Bogus;
global using Bogus.DataSets;

global using DocSpace.API.SDK.Api;
global using DocSpace.API.SDK.Api.Files;
global using DocSpace.API.SDK.Api.Rooms;
global using DocSpace.API.SDK.Client;
global using DocSpace.API.SDK.Model;

global using DotNet.Testcontainers.Containers;

global using FluentAssertions;

global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Mvc.Testing;
global using Microsoft.AspNetCore.TestHost;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

global using MySql.Data.MySqlClient;

global using Newtonsoft.Json.Linq;

global using Npgsql;

global using Respawn;
global using Respawn.Graph;

global using Testcontainers.MySql;
global using Testcontainers.OpenSearch;
global using Testcontainers.PostgreSql;
global using Testcontainers.RabbitMq;
global using Testcontainers.Redis;

global using Xunit;

global using ApiDateTime = DocSpace.API.SDK.Model.ApiDateTime;
global using CreateFolder = DocSpace.API.SDK.Model.CreateFolder;
global using CreateRoomRequestDto = DocSpace.API.SDK.Model.CreateRoomRequestDto;
global using ExternalShareRequestParam = DocSpace.API.SDK.Model.ExternalShareRequestParam;
global using FileLinkRequest = DocSpace.API.SDK.Model.FileLinkRequest;
global using FileOperationDto = DocSpace.API.SDK.Model.FileOperationDto;
global using FileShare = DocSpace.API.SDK.Model.FileShare;
global using FileShareDto = DocSpace.API.SDK.Model.FileShareDto;
global using FolderType = DocSpace.API.SDK.Model.FolderType;
global using RoomType = DocSpace.API.SDK.Model.RoomType;
global using User = ASC.Files.Tests.Data.User;

global using WebApiProgram = ASCWebApi::Program;
global using FilesProgram = ASCFiles::Program;
global using PeopleProgram = ASCPeople::Program;
global using FilesServiceProgram = ASCFilesService::Program;
global using LinkType = DocSpace.API.SDK.Model.LinkType;
global using Task = System.Threading.Tasks.Task;
