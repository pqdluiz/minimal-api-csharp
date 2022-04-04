using MinimalApi.Services;

var builder = new Builder();
var app = builder.Build(args).Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo Api v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseWebSockets();

app.UseRouting();
app.UseAuthorization();
app.UseAuthentication();
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
});

app.Run();





