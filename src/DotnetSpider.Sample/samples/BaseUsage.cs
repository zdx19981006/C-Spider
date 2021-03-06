using System.Collections.Generic;
using System.Threading.Tasks;
using DotnetSpider.Data;
using DotnetSpider.Data.Parser;
using DotnetSpider.Data.Storage;
using DotnetSpider.Downloader;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace DotnetSpider.Sample.samples
{
	public class BaseUsage
	{
		public static Task Run()
		{
			var builder = new SpiderHostBuilder()
				.ConfigureLogging(x => x.AddSerilog())
				.ConfigureAppConfiguration(x => x.AddJsonFile("appsettings.json"))
				.ConfigureServices(services =>
				{
					services.AddLocalMessageQueue();
					services.AddLocalDownloaderAgent(x =>
					{
						x.UseFileLocker();
						x.UseDefaultAdslRedialer();
						x.UseDefaultInternetDetector();
					});
					services.AddLocalDownloadCenter();
					services.AddSpiderStatisticsCenter(x => x.UseMemory());
				});
			var provider = builder.Build();

			var spider = provider.Create<Spider>();
			spider.NewGuidId(); // 设置任务标识
			spider.Name = "博客园全站采集"; // 设置任务名称
			spider.Speed = 1; // 设置采集速度, 表示每秒下载多少个请求, 大于 1 时越大速度越快, 小于 1 时越小越慢, 不能为0.
			spider.Depth = 3; // 设置采集深度
			spider.DownloaderSettings.Type = DownloaderType.HttpClient; // 使用普通下载器, 无关 Cookie, 干净的 HttpClient
			spider.AddDataFlow(new CnblogsDataParser()).AddDataFlow(new ConsoleStorage());
			spider.AddRequests(new Request("http://www.cnblogs.com/", new Dictionary<string, string>
			{
				{"key1", "value1"}
			})); // 设置起始链接
			return spider.RunAsync(); // 启动
		}

		class CnblogsDataParser : DataParser
		{
			public CnblogsDataParser()
			{
				CanParse = DataParserHelper.CanParseByRegex("cnblogs\\.com");
				QueryFollowRequests = DataParserHelper.QueryFollowRequestsByXPath(".");
			}

			protected override Task<DataFlowResult> Parse(DataFlowContext context)
			{
				context.AddItem("URL", context.Response.Request.Url);
				context.AddItem("Title", context.GetSelectable().XPath(".//title").GetValue());
				return Task.FromResult(DataFlowResult.Success);
			}
		}
	}
}