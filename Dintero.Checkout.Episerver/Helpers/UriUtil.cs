using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace Dintero.Checkout.Episerver.Helpers
{
    public static class UriUtil
    {
        private static readonly Injected<IContentLoader> ContentLoader;
        private static readonly Injected<UrlResolver> UrlResolver;
        static UriUtil()
        {
            UrlResolver = default;
            ContentLoader = default;
        }

        /// <summary>
        /// Add query parameter to url
        /// </summary>
        /// <param name="url"></param>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string AddQueryString(string url, string name, string val)
        {
            if (name.StartsWith("?") || name.StartsWith("&"))
            {
                name = name.Remove(0, 1);
            }

            if (!name.EndsWith("="))
            {
                name += "=";
            }

            int num = url.IndexOf('?');

            if (num < 0)
            {
                return url + "?" + name + val;
            }

            var strArray = url.Substring(num + 1).Split('&');
            for (int index1 = 0; index1 < strArray.Length; ++index1)
            {
                if (strArray[index1].StartsWith(name))
                {
                    strArray[index1] = name + val;
                    var str = string.Empty;
                    for (var index2 = 0; index2 < strArray.Length; ++index2)
                    {
                        str = index2 != 0 ? str + "&" + strArray[index2] : str + strArray[index2];
                    }

                    return url.Substring(0, num + 1) + str;
                }
            }

            return url + "&" + name + val;
        }

        /// <summary>
        /// Gets url from start page's reference property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="isAbsolute">Whether to return relative or absolute url.</param>
        /// <returns>The friendly url.</returns>
        public static string GetUrlFromStartPageReferenceProperty(string propertyName, bool isAbsolute = false)
        {
            var url = UrlResolver.Service.GetUrl(ContentReference.StartPage);

            var startPageData = ContentLoader.Service.Get<PageData>(ContentReference.StartPage);
            if (startPageData != null)
            {
                var contentLink = startPageData.Property[propertyName]?.Value as ContentReference;
                if (!ContentReference.IsNullOrEmpty(contentLink))
                {
                    url = UrlResolver.Service.GetUrl(contentLink);
                }
            }

            if (isAbsolute)
            {
                url = UriSupport.AbsoluteUrlBySettings(url);
            }

            return url;
        }

        public static string GetBaseUrl()
        {
            return UriSupport.AbsoluteUrlBySettings(UrlResolver.Service.GetUrl(ContentReference.StartPage));
        }
    }
}