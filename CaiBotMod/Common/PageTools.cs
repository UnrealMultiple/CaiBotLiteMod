using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Color = System.Drawing.Color;

namespace CaiBotMod.Common;

/// <summary>
///     Provides tools for sending paginated output.
/// </summary>
public static class PaginationTools
{
    public delegate Tuple<string?, Color> LineFormatterDelegate(object lineData, int lineIndex, int pageNumber);

    public static void SendPage(
        TSPlayer player, int pageNumber, IEnumerable dataToPaginate, int dataToPaginateCount, Settings? settings = null)
    {
        settings ??= new Settings();

        if (dataToPaginateCount == 0)
        {
            if (!player.RealPlayer)
            {
                player.SendSuccessMessage(settings.NothingToDisplayString);
            }
            else
            {
                player.SendMessage(settings.NothingToDisplayString, settings.HeaderTextColor);
            }

            return;
        }

        var pageCount = ((dataToPaginateCount - 1) / settings.MaxLinesPerPage) + 1;
        if (settings.PageLimit > 0 && pageCount > settings.PageLimit)
        {
            pageCount = settings.PageLimit;
        }

        if (pageNumber > pageCount)
        {
            pageNumber = pageCount;
        }

        if (settings.IncludeHeader)
        {
            if (!player.RealPlayer)
            {
                player.SendSuccessMessage(string.Format(settings.HeaderFormat, pageNumber, pageCount));
            }
            else
            {
                player.SendMessage(string.Format(settings.HeaderFormat, pageNumber, pageCount),
                    settings.HeaderTextColor);
            }
        }

        var listOffset = (pageNumber - 1) * settings.MaxLinesPerPage;
        var offsetCounter = 0;
        var lineCounter = 0;
        foreach (var lineData in dataToPaginate)
        {
            if (lineData == null)
            {
                continue;
            }

            if (offsetCounter++ < listOffset)
            {
                continue;
            }

            if (lineCounter++ == settings.MaxLinesPerPage)
            {
                break;
            }

            string? lineMessage;
            var lineColor = settings.LineTextColor;
            if (lineData is Tuple<string, Color> data)
            {
                lineMessage = data.Item1;
                lineColor = data.Item2;
            }
            else if (settings.LineFormatter != null)
            {
                try
                {
                    var lineFormat = settings.LineFormatter(lineData, offsetCounter, pageNumber);

                    lineMessage = lineFormat.Item1;
                    lineColor = lineFormat.Item2;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        "LineFormatter引用的方法引发了异常。有关详细信息，请参阅内部异常.", ex);
                }
            }
            else
            {
                lineMessage = lineData.ToString();
            }

            if (lineMessage != null)
            {
                if (!player.RealPlayer)
                {
                    player.SendInfoMessage(lineMessage);
                }
                else
                {
                    player.SendMessage(lineMessage, lineColor);
                }
            }
        }

        if (lineCounter == 0)
        {
            if (settings.NothingToDisplayString != null)
            {
                if (!player.RealPlayer)
                {
                    player.SendSuccessMessage(settings.NothingToDisplayString);
                }
                else
                {
                    player.SendMessage(settings.NothingToDisplayString, settings.HeaderTextColor);
                }
            }
        }
        else if (settings.IncludeFooter && pageNumber + 1 <= pageCount)
        {
            if (!player.RealPlayer)
            {
                player.SendInfoMessage(string.Format(settings.FooterFormat, pageNumber + 1, pageNumber, pageCount));
            }
            else
            {
                player.SendMessage(string.Format(settings.FooterFormat, pageNumber + 1, pageNumber, pageCount),
                    settings.FooterTextColor);
            }
        }
    }

    public static void SendPage(TSPlayer player, int pageNumber, IList dataToPaginate, Settings? settings = null)
    {
        SendPage(player, pageNumber, dataToPaginate, dataToPaginate.Count, settings);
    }

    public static List<string> BuildLinesFromTerms(IEnumerable terms, Func<object, string> termFormatter = null!,
        string separator = ", ", int maxCharsPerLine = 80)
    {
        List<string> lines = new ();
        StringBuilder lineBuilder = new ();

        foreach (var term in terms)
        {
            if (term == null && termFormatter == null)
            {
                continue;
            }

            string? termString = null!;
            if (termFormatter != null)
            {
                try
                {
                    if (term != null && (termString = termFormatter(term)) == null)
                    {
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    throw new ArgumentException(
                        "由termFormatter表示的方法抛出了异常。详情请参阅内部异常.", ex);
                }
            }
            else
            {
                termString = term?.ToString();
            }

            Debug.Assert(termString != null, nameof(termString) + " != null");
            if (lineBuilder.Length + termString.Length + separator.Length < maxCharsPerLine)
            {
                lineBuilder.Append(termString).Append(separator);
            }
            else
            {
                lines.Add(lineBuilder.ToString());
                lineBuilder.Clear().Append(termString).Append(separator);
            }
        }

        if (lineBuilder.Length > 0)
        {
            lines.Add(lineBuilder.ToString().Substring(0, lineBuilder.Length - separator.Length));
        }

        return lines;
    }

    public static bool TryParsePageNumber(List<string> commandParameters, int expectedParameterIndex,
        TSPlayer errorMessageReceiver, out int pageNumber)
    {
        pageNumber = 1;
        if (commandParameters.Count <= expectedParameterIndex)
        {
            return true;
        }

        var pageNumberRaw = commandParameters[expectedParameterIndex];
        if (!int.TryParse(pageNumberRaw, out pageNumber) || pageNumber < 1)
        {
            errorMessageReceiver.SendErrorMessage("\"{0}\" 不是个有效的页码.", pageNumberRaw);

            pageNumber = 1;
            return false;
        }

        return true;
    }

    #region [Nested: Settings Class]

    public class Settings
    {
        private readonly string _footerFormat = "输/<command> {{0}} 翻页.";

        private readonly string _headerFormat = "页码 {{0}}/{{1}}";

        private int _maxLinesPerPage = 4;

        private int _pageLimit;


        public bool IncludeHeader { get; set; } = true;

        public string HeaderFormat
        {
            get => this._headerFormat;
            init => this._headerFormat = value ?? throw new ArgumentNullException();
        }

        public Color HeaderTextColor { get; set; } = Color.Green;
        public bool IncludeFooter { get; set; } = true;

        public string FooterFormat
        {
            get => this._footerFormat;
            init => this._footerFormat = value ?? throw new ArgumentNullException();
        }

        public Color FooterTextColor { get; set; } = Color.Yellow;
        public string? NothingToDisplayString { get; set; } = null;
        public LineFormatterDelegate LineFormatter { get; set; } = null!;
        public Color LineTextColor { get; set; } = Color.Yellow;

        public int MaxLinesPerPage
        {
            get => this._maxLinesPerPage;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("值不能为0.");
                }

                this._maxLinesPerPage = value;
            }
        }

        public int PageLimit
        {
            get => this._pageLimit;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("值不能为0.");
                }

                this._pageLimit = value;
            }
        }
    }

    #endregion
}