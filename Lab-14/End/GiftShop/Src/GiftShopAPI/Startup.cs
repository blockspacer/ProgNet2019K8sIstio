﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

using Autofac;
using Autofac.Extensions.DependencyInjection;
using Autofac.Extras.DynamicProxy;

using GiftShopAPI.Middleware;
using GiftShopAPI.Controllers;

namespace GiftShopAPI
{
    public class Startup
    {

        public IContainer ApplicationContainer { get; private set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
 
            // Register the Swagger generator, defining 1 or more Swagger documents
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(Configuration.GetValue<string>("App:Version"), 
                    new OpenApiInfo { 
                        Title = Configuration.GetValue<string>("App:Title"), 
                        Version = Configuration.GetValue<string>("App:Version")
                        });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });

            services.AddHttpClient();

            services.AddTransient<IstioTracingMiddleware>();

            //Autofac Changes
            services.AddAutofac();

            //Create Container builder
            var builder = new ContainerBuilder();

            //Populate Services from Microsoft DI
            builder.Populate(services);

            //Register DI types or Interfaces
            //add Interceptions by calling Extension metods EnableClassInterceptors or EnableInterfaceInterceptors
            builder.RegisterType<GiftsController>().EnableClassInterceptors();
            
            //Register Interceptor
            builder.RegisterType<CallLogger>();

            //Build Container
            this.ApplicationContainer = builder.Build();

            //Return Autofac provider as Serviceprovider
            return new AutofacServiceProvider(this.ApplicationContainer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/{Configuration.GetValue<string>("App:Version")}/swagger.json", 
                    $"{Configuration.GetValue<string>("App:Title")} {Configuration.GetValue<string>("App:Version")}");
            });

            app.UseHttpsRedirection();

            app.UseIstioTracingMiddleware();

            app.UseMvc();
        }
    }
}
