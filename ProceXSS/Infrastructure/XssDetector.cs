using System;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using ProceXSS.Configuration;
using ProceXSS.Enums;
using ProceXSS.Interface;
using ProceXSS.Log;
using ProceXSS.Struct;

namespace ProceXSS.Infrastructure
{
    public class XssDetector : IXssDetector
    {
        private readonly IXssConfigurationHandler _configuration;
        private readonly IRegexProcessor _regexProcessor;
        private readonly ILogger _logger;
        private Regex _xssDetectionRegex;

        public XssDetector(IXssConfigurationHandler configuration, IRegexProcessor regexProcessor,ILogger logger)
        {
            _configuration = configuration;
            _regexProcessor = regexProcessor;
            _logger = logger;
        }

        public RequestValidationResult HasXssVulnerability(HttpRequest request)
        {
            if (string.IsNullOrWhiteSpace(_configuration.ControlRegex))
            {
                _xssDetectionRegex = new Regex(_regexProcessor.XssPattern, RegexOptions.IgnoreCase);
            }
            else
            {
                try
                {
                    _xssDetectionRegex = new Regex(HttpUtility.HtmlDecode(_configuration.ControlRegex), RegexOptions.IgnoreCase);
                }
                catch
                {
                    _xssDetectionRegex = new Regex(_regexProcessor.XssPattern, RegexOptions.IgnoreCase);
                }
            }

            RequestValidationResult result = new RequestValidationResult
            {
                IsValid = true,
                DiseasedRequestPart = DiseasedRequestPart.None
            };

            if (request != null)
            {
                string queryString = request.QueryString.ToString();

                if (!string.IsNullOrEmpty(queryString) &&
                    _regexProcessor.ExecFor(_xssDetectionRegex, queryString))
                {
                    result.IsValid = false;
                    result.DiseasedRequestPart = DiseasedRequestPart.QueryString;
                }

                if (request.HttpMethod.Equals("POST", StringComparison.InvariantCultureIgnoreCase))
                {
                    string formPostValues;

                    try
                    {
                        formPostValues = request.Form.ToString();
                    }
                    catch (Exception ex)
                    {
                        if (_configuration.Log.Equals(bool.TrueString))
                        {
                            string message = string.Format(@"Request.Form getter called, Method :{0}, Requested Page: {1}", MethodBase.GetCurrentMethod().Name, request.Url);
                            _logger.Error(message, ex);
                        }

                        throw;
                    }


                    if (!string.IsNullOrEmpty(formPostValues) && _regexProcessor.ExecFor(_xssDetectionRegex, formPostValues))
                    {
                        result.IsValid = false;
                        result.DiseasedRequestPart = DiseasedRequestPart.Form;
                    }
                }
            }

            return result;
        }
    }
}