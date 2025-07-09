using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using website.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace website
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSingleton<Users.UserStore>();

			// Add cookie authentication
			builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Admin/Login";
                    options.LogoutPath = "/Admin/Logout";
                    options.AccessDeniedPath = "/Admin/AccessDenied";
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Admin/Error");
            }

            app.UseRouting();

            // Enable authentication and authorization middleware
            app.UseAuthentication();
            app.UseAuthorization();

            StaticWebAssetsLoader.UseStaticWebAssets(configuration: builder.Configuration, environment: app.Environment);

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
