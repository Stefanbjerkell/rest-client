﻿# RestClient

This rest client was created to add some extra features to the standard System.Net.Http.HttpClient.

Features.
- Fluid way of adding headers/query-parameters/url-parameters/body
- Request and response body parsing.
- Error handling with easy to use fluid syntax or possibility to add custom error handlers.
- Mocking tool for easy set up of mocked httpClients for Unit tests.

## Getting started

#### Simple version

```
var client = new HttpRestClient("https://myapi.com");

var request = client.Get("path");

var response = await request.Execute();
```

Or just 
```
var client = new HttpRestClient("https://myapi.com");

var response = client.Get("path").Execute();
```

#### Other versions

You can also set up the client by providing a settings object with some additional settings for your client
```
var settings = new RestClientSettings()
{
	BaseUrl = "http://myapi.com",
	TimeOutInSeconds = 10,
	UserAgent = "Agent1"
}
var client = new HttpRestClient(settings);
```

The above constructors will create a HttpClient internaly that will be used to do the actual requests. If you want to you can inject your own HttpClient or HttpMessageHandler that will be used instead.
This can be good when doing unit tests and you want to mock the HttpRequest.
```
var myHttpClient = new HttpClient();
var client = new HttpRestClient(myHttpClient);
```
Or..

```
var myHttpMessagehandler = new HttpMessageHandler();
var client = new HttpRestClient(myHttpMessagehandler);
```

#### Headers
```
var client = new HttpRestClient("https://myapi.com);

var request = client.Get("path")
	.WithHeader("My-Header-1", "header-value")
	.WithHeader("My-Header-2", "header-value");

var response = await request.Execute();
```

> 💡 You can also add default headers directly to the client. with AddDefaultHeader(name,value) method. Or add them directly to the dictionary at HttpRestClient.DefaultHeaders

##### Auth headers

If you want to add authorization to your client you can use the following 2 methods on the client.
```
client.AddAuthToken(token, "Bearer");
// OR
client.AddBasicAuth(username, password);
```
> 💡 on AddAuthToken "Bearer" is default value so you don't have to include that.


#### Request/Response body
```
var client = new HttpRestClient("https://myapi.com);
var body = new MyBodyObject();

var request = client.Post("path")
	.WithJsonBody(body);

var response = await request.Execute();

var data = response.Content<ResponseContenetType>();
```

As you can see you get the parsed response by calling response.Content<Type\>()

You can also pass the body directly in the Post/Put method.
```
var request = client.Post("path", body)
```
#### Query and route parameters
```
var client = new HttpRestClient("https://myapi.com);
var accountId = "my-account-id";

var request = client.Get("account/{accountId}")
	.WithParameter(accountId) // This is equal to ("accountId", accountId)
	.WithQueryParameter("query", "value");

var response = await request.Execute();
```
>💡 WithParameter will replace the {account} placeholder from the path. As you see in the example the name of the parameter will be used as the "key" if no key is provided.

## Error Handling

To deal with errors you can attach a ErrorHandler to the request. Error handlers can be custom built or you can use 2 of the default ones that comes with this package.

Examples of how to attach the default error handlers.

#### Catch all errors.

```
var client = new HttpRestClient("https://myapi.com);
var accountId = "my-account-id";

var request = client.Get("accounts/{accountId}")
	.OnError()
		.Do(async (request, response) => { // Do something with the response here });

var response = await request.Execute();
```

#### Catch specific HttpStatus.

```
var client = new HttpRestClient("https://myapi.com);
var accountId = "my-account-id";

var request = client.Get("accounts/{accountId}")
	.OnError(HttpStatusCode.NotFound)
		.Throw(new AccountNotFoundException());

var response = await request.Execute();
```

As you see you can add the default handlers in a fluid way by firs specifying the condition for the error handler to trigger on. Then you can specify the action to take,
The default error handler have the 3 following actions available.

- Throw(Exception ex) // Throw you own exception.
- SetBody(HttpStatusCode status, string content) // Override the response status and body.
- Do(HandleError handleError) // Do whatever you want.

The Do action takes a delegate method that looks like this.
```
delegate Task HandleError(HttpResponseMessage response, RestClientResponse result)
```
This has the incoming httpResponse and also the RestClientResponse as input parameters. Here you can do what you want. For example modify the response or do some logging.

#### Add custom error handler.

```
var client = new HttpRestClient("https://myapi.com);
var accountId = "my-account-id";

var handler = new CustomErrorHandler();

var request = client.Get("accounts/{accountId}")
	.AddErrorHandler(handler);

var response = await request.Execute();
```

Custom error handler must inplement the interface IErrorHandler. It has two methods.

- bool CanHandle(HttpResponseMessage httpResponse)
- Task Handle(HttpResponseMessage httpResponse, RestClientResponse response)

CanHandle lets the rest client know if the handler can handle the error. If multiple errorHandlers can handle the error the first one added will take priority. So the order in wich you add them will matter (if you have more then one).

To handle a error you provide a method that takes the original httpResponse and the expected RestClientResposne as parameters. If you want to you can here modify the RestClientResponse or just do some logging/throwing a exception, its up to you.

## RestClient.Mock

Together with the RestClient package there is also the RestClient.Mock package that has some nice mocking helpers for setting up mocked http-clients.
This can be handy when writing unit tests.

Examples.
```
var body = MyResponseBody();

var httpClient = HttpMock.SetupClient()
	.Get("good-path", HttpStatusCode.OK, body)
	.Post("bad-path", HttpStatusCode.BadRequest)
	.Build();
```

This will set up a httpClient with two mocked path.One that will return OK and another that will return a error.

If you leave out the path parameter it will default to "*" (catching all paths).

You can also choose to set a default response to any request to the client.

```

var httpClient = HttpMock.SetupClient()
	.WithDefaultResponse(HttpStatusCode.OK)
	.Build();
```

You can provide a delay to any of the mocked responses if you want to.

```
var delayInMilliseconds = 20000;
var body = MyResponseBody();

var httpClient = HttpMock.SetupClient()
	.WithDefaultResponse(HttpStatusCode.OK, body, delayInMilliseconds)
	.Build();
```   

If you want more advanced mocking you can provide the full HttpResponseMessage instead of just HttpStatus and Body.
```
var myResponse = new HttpResponseMessage();

var httpClient = HttpMock.SetupClient()
	.WithDefaultResponse(myResponse)
	.Build();
```

The mocked HttpClient can then easily be injected in to the HttpRestClient like this.
```
var mockedHttpClient = HttpMock.SetupClient()
	.WithDefaultResponse(myResponse)
	.Build();

var restClient = new HttpRestClient(mockedHttpClient);

```

## Advanced setup.

### Inherit from HttpRestClient

The way i normally use this client is to create my Repository or Api class that inherits from HttpRestClient.


```
public class WeatherApi : HttpRestClient
{
	public WeatherApi(IOption<WeatherApiSettings> settings) : base(settings.value)
	{
	}

	public async Task<Weather> GetWeather(string city, int days)
	{
		var request = Get("weather/{city})
			.WithParameter(city)
			.WithQueryParameter(days)
			.OnError(HttpStatusCode.NotFound)
				.SetResponse(200, "{}")
			.OnError(HttpStatusCode.BadRequest)
				.Throw(new Exception("Something went wrong calling weather service");

		return await request.ExecuteAsData<Weather>();
	}

}

public class WeatherApiSettings : RestClientSettings
{
	public const string Position = "WeatherApi";
}
```

In this example we have a WeatherApi class that inherits from HttpRestClient. 

It has 2 error handlers registered for diffrent status codes and we choose the ExecuteAsData to directly return the deserialized content.

We also have to setup dependency injection for the WeatherApiSettings and have it map to a section in appsettings with all our configuration.

```
builder.Services.AddOptions<WeatherApiSettings>()
    .Bind(builder.Configuration.GetSection(WeatherApiSettings.Position))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

In this example we are using the AddOptions method to add the IOption\<WeatherService> interface to DI. 
I like this way because it can also run validations for your data annotaions in your settings object.

## Json Serializers

By default the HttpRestClient will use the built in System.Text.Json.JsonSerializer to serialize/deserialize objects.

There are however options to override this if you want to use for example Newtonsoft.

All construcors on the HttpRestClient have the possibility to include/inject a optional IRestClientSerializer. 
If you provice a serializer here it will be used instad of the default one.

> 💡 If you are happy wiht the .NET serializer but want to change the json options you can add a new DefaultSerializer here with your own settings.

If you want to do this on a global level for all your HttpRestClient's you can register it in your program.cs/starup.cs like this,

Example.
```
services.AddHttpRestClient()
	.AddDefaultSerializer(new JsonSerializerOptions());
```

You can also directly replace the DefaultSerializer on the static field HttpRestClient.DefaultSerializer
```
var myOptions = new JsonSerializerOptions()

HttpRestClient.DefaultSerializer = new DefaultSerializer(myOptions)
```

#### NewtonsoftSerializer

I also created a Newtonsoft serializer that you can use by adding the RestClient.Serializers.Newtonsoft nuget package.
You can then add it as the default serializer like this.
```
services.AddHttpRestClient()
	.AddNewtonsoftDefaultSerializer(new JsonSerializerSettings());
```

Or if you want to have it only in a specific client you can provide it when creating it like this.
```
var serializer = new NewtonsoftSerializer(new JsonSerializerSettings());
var client = new HttpRestClient("MyUrl", serializer);
// OR
client = new HttpRestClient("MyUrl")
{
	Serializer = serializer
}
```
Alternatively if you create your clients from dependency injection you can also add the IRestClientSerializer interface to DI and resolve it to the serializer of your preference.