﻿extern alias ASCWebApi;
extern alias ASCFiles;
extern alias ASCPeople;
extern alias ASCFilesService;
global using System.Data.Common;
global using System.Net;
global using System.Net.Http.Headers;
global using System.Text;
global using System.Text.Json;
global using System.Web;

global using ASC.Core.Common.EF;
global using ASC.Files.Core.ApiModels.ResponseDto;
global using ASC.Files.Tests.Data;
global using ASC.Files.Tests.Factory;
global using ASC.Migrations;
global using ASC.Migrations.Core;

global using Bogus;
global using Bogus.DataSets;

global using Docspace.Api;
global using Docspace.Client;
global using Docspace.Model;

global using DotNet.Testcontainers.Builders;
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

global using FileOperationDto = Docspace.Model.FileOperationDto;
global using FileShare = Docspace.Model.FileShare;
global using WebApiProgram = ASCWebApi::Program;
global using FilesProgram = ASCFiles::Program;
global using PeopleProgram = ASCPeople::Program;
global using FilesServiceProgram = ASCFilesService::Program;
global using FolderType = Docspace.Model.FolderType;
global using User = ASC.Files.Tests.Data.User;