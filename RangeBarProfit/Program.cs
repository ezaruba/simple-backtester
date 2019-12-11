﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using RangeBarProfit.Strategies;

namespace RangeBarProfit
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine($"Range bars profit computer");

            var baseDir = "C:\\dev\\work\\manana\\data\\bitmex\\price-range-bars\\2019-11";
            var baseDirBitfinex = "C:\\dev\\work\\manana\\data-processed\\bitfinex";

            var backtests = new[]
            {
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = baseDirBitfinex,
                    FilePattern = "bitfinex_price-range-bars-004_btcusd_*.csv",
                    Range = "004",
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = baseDirBitfinex,
                    FilePattern = "bitfinex_price-range-bars-002_btcusd_*.csv",
                    Range = "002",
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "BTC",
                    QuoteSymbol = "USD",
                    Amount = 1,
                    DirectoryPath = baseDirBitfinex,
                    FilePattern = "bitfinex_price-range-bars-001_btcusd_*.csv",
                    Range = "001",
                    Visualize = true
                },

                new BacktestConfig
                {
                    BaseSymbol = "ETH",
                    QuoteSymbol = "USD",
                    Amount = 10,
                    DirectoryPath = baseDirBitfinex,
                    FilePattern = "bitfinex_price-range-bars-004_ethusd_*.csv",
                    Range = "004",
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "ETH",
                    QuoteSymbol = "USD",
                    Amount = 10,
                    DirectoryPath = baseDirBitfinex,
                    FilePattern = "bitfinex_price-range-bars-002_ethusd_*.csv",
                    Range = "002",
                    Visualize = true
                },
                new BacktestConfig
                {
                    BaseSymbol = "ETH",
                    QuoteSymbol = "USD",
                    Amount = 10,
                    DirectoryPath = baseDirBitfinex,
                    FilePattern = "bitfinex_price-range-bars-001_ethusd_*.csv",
                    Range = "001",
                    Visualize = true
                },

                //new BacktestConfig
                //{
                //    BaseSymbol = "BTC",
                //    QuoteSymbol = "USD",
                //    Amount = 1,
                //    DirectoryPath = Path.Combine(baseDir, "xbtusd-002"),
                //    Visualize = true
                //},
                //new BacktestConfig
                //{
                //    BaseSymbol = "BTC",
                //    QuoteSymbol = "USD",
                //    Amount = 1,
                //    DirectoryPath = Path.Combine(baseDir, "xbtusd-001"),
                //    Visualize = true
                //},
                //new BacktestConfig
                //{
                //    BaseSymbol = "BTC",
                //    QuoteSymbol = "USD",
                //    Amount = 1,
                //    DirectoryPath = Path.Combine(baseDir, "xbtusd-0005"),
                //    Visualize = false
                //},
                //new BacktestConfig
                //{
                //    BaseSymbol = "BTC",
                //    QuoteSymbol = "USD",
                //    Amount = 1,
                //    DirectoryPath = Path.Combine(baseDir, "xbth20-002"),
                //    Visualize = true
                //},
                //new BacktestConfig
                //{
                //    BaseSymbol = "BTC",
                //    QuoteSymbol = "USD",
                //    Amount = 1,
                //    DirectoryPath = Path.Combine(baseDir, "xbth20-001"),
                //    Visualize = true
                //},
                //new BacktestConfig
                //{
                //    BaseSymbol = "BTC",
                //    QuoteSymbol = "USD",
                //    Amount = 1,
                //    DirectoryPath = Path.Combine(baseDir, "xbth20-0005"),
                //    Visualize = false
                //},
                //new BacktestConfig
                //{
                //    BaseSymbol = "ETH",
                //    QuoteSymbol = "USD",
                //    Amount = 10,
                //    DirectoryPath = Path.Combine(baseDir, "ethusd-002"),
                //    Visualize = true
                //},
                //new BacktestConfig
                //{
                //    BaseSymbol = "ETH",
                //    QuoteSymbol = "USD",
                //    Amount = 10,
                //    DirectoryPath = Path.Combine(baseDir, "ethusd-001"),
                //    Visualize = true
                //},
                //new BacktestConfig
                //{
                //    BaseSymbol = "ETH",
                //    QuoteSymbol = "USD",
                //    Amount = 10,
                //    DirectoryPath = Path.Combine(baseDir, "ethusd-0005"),
                //    Visualize = false
                //},
            };



            foreach (var backtest in backtests)
            {
                //var strategy = new NaiveStrategy();
                var strategy = new TrendStrategy(true);
                //var strategy = new NaiveFollowerStrategy(true);

                RunBacktest(backtest, strategy);
            }
        }

        private static void RunBacktest(BacktestConfig backtest, IStrategy strategy)
        {
            var files = LoadAllFiles(backtest.DirectoryPath, backtest.FilePattern);
            if (backtest.SkipFiles.HasValue)
                files = files.Skip(backtest.SkipFiles.Value).ToArray();
            if (backtest.LimitFiles.HasValue)
                files = files.Take(backtest.LimitFiles.Value).ToArray();

            if (!files.Any())
                return;

            Console.WriteLine();
            Console.WriteLine("=====================================================================");
            Console.WriteLine($"    Running for {files.Length} files from dir '{backtest.DirectoryPath}' and pattern: '{backtest.FilePattern}'");

            var builder = new StringBuilder();

            foreach (var maxInventory in backtest.MaxInventory)
            {
                RangeBarModel lastBar = null;
                var computer = new ProfitComputer(strategy, backtest, maxInventory);
                foreach (var file in files)
                {
                    var bars = LoadBars(file);
                    computer.ProcessBars(bars);
                    lastBar = bars.LastOrDefault();
                }

                if (lastBar != null)
                    computer.ProcessLastBar(lastBar);

                var report = computer.GetReport();
                builder.AppendLine(report);
                Console.WriteLine($"    {report}");

                var perMonth = computer.GetReportByMonth();
                foreach (var month in perMonth)
                {
                    builder.AppendLine($"    {month}");
                }
                builder.AppendLine();

                Visualize(backtest, computer, strategy, maxInventory);
            }

            SaveTextReport(builder.ToString(), backtest, strategy);

            Console.WriteLine();
        }

        private static RangeBarModel[] LoadBars(string file)
        {
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader);
            csv.Configuration.PrepareHeaderForMatch = (header, index) => header.ToLower();
            return csv.GetRecords<RangeBarModel>().ToArray();
        }

        private static string[] LoadAllFiles(string dirPath, string filePattern)
        {
            if(!Directory.Exists(dirPath))
                return new string[0];

            var files = Directory.EnumerateFiles(dirPath, filePattern);
            return files.OrderBy(x => x).ToArray();
        }

        private static void SaveTextReport(string report, BacktestConfig backtest, IStrategy strategy)
        {
            var filename = Path.GetFileName(backtest.DirectoryPath);
            var strategyName = strategy.GetType().Name;
            var pattern = ExtractFromPattern(backtest);
            pattern = string.IsNullOrWhiteSpace(pattern) ? filename : pattern;
            var targetFile = Path.Combine(GetPathToReportDir(backtest), $"{pattern}__{strategyName}.txt");
            File.WriteAllText(targetFile, report);
        }

        private static void Visualize(BacktestConfig backtest, ProfitComputer computer, IStrategy strategy, int maxInv)
        {
            if (computer == null || !backtest.Visualize)
                return;

            var chart = new ChartVisualizer();
            var filename = Path.GetFileName(backtest.DirectoryPath);
            var strategyName = strategy.GetType().Name;
            var pnl = computer.GetPnl();
            var name = $"{filename} {strategyName} (max inv: {maxInv}) pnl: {pnl:#.00} {backtest.QuoteSymbol}";
            var dir = GetPathToReportDir(backtest);
            var targetFile = Path.Combine(dir, $"{filename}__{strategyName}__{backtest.Range}__{maxInv}__{pnl:0}");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var bars = computer.Bars;
            if (backtest.VisualizeSkipBars.HasValue)
                bars = bars.Skip(backtest.VisualizeSkipBars.Value).ToArray();
            if (backtest.VisualizeLimitBars.HasValue)
                bars = bars.Take(backtest.VisualizeLimitBars.Value).ToArray();

            var minIndex = bars.Min(x => x.Index);
            var maxIndex = bars.Max(x => x.Index);

            var trades = computer.Trades
                .Where(x => x.BarIndex >= minIndex && x.BarIndex <= maxIndex)
                .ToArray();

            chart.PlotTrades(name, targetFile, bars, trades, backtest.VisualizeByTime);
        }

        private static string GetPathToReportDir(BacktestConfig backtest)
        {
            return Path.Combine(Path.GetDirectoryName(backtest.DirectoryPath), "reports");
        }

        private static string ExtractFromPattern(BacktestConfig backtest)
        {
            var pattern = backtest.FilePattern ?? string.Empty;
            var split = pattern.Split("*");
            if (split.Length > 1)
                return split[0]
                    .Trim('_')
                    .Trim('_')
                    .Trim('/')
                    .Trim('\\')
                    .Trim();
            return pattern;
        }
    }
}
