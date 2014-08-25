using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using System.ComponentModel;
using IMDb_Scraper;
using System.Threading;
using System.Threading.Tasks;
#region
//Original: http://web3o.blogspot.com/2010/11/aspnetc-imdb-scraping-api.html Last Updated: Feb 20, 2013
#endregion

namespace IMDb_Scraper
{
    internal interface IIMDb
    {
    }
    /// <summary>
    /// La clase implementan el interface, de tal forma que COM sólo mostrará estas clases
    /// </summary>
    [Description("Clase con los detalles en IMDb de una película")]
    [ClassInterface(ClassInterfaceType.None)]
    //[DefaultProperty("Título")]//TODO don't work

    internal class IMDb : IIMDb
    {
        #region public & private definitions
        public bool status { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string OriginalTitle { get; set; }
        public string Year { get; set; }
        public string Rating { get; set; }
        public ArrayList Genres { get; set; }
        public ArrayList Directors { get; set; }
        public ArrayList Writers { get; set; }
        public ArrayList namesCast { get; set; }
        public ArrayList FullCast { get; set; }
        public ArrayList Producers { get; set; }
        public ArrayList Musicians { get; set; }
        public ArrayList Cinematographers { get; set; }
        public ArrayList Editors { get; set; }
        public string MpaaRating { get; set; }
        public string ReleaseDate { get; set; }
        public string Plot { get; set; }
        public ArrayList PlotKeywords { get; set; }
        public string Poster { get; set; }
        public string Runtime { get; set; }
        public string Top250 { get; set; }
        public string Oscars { get; set; }
        public string Awards { get; set; }
        public string Nominations { get; set; }
        public string Tagline { get; set; }
        public string Votes { get; set; }
        public ArrayList Languages { get; set; }
        public ArrayList Countries { get; set; }
        public ArrayList ReleaseDates { get; set; }
        public string MediaImages { get; set; }
        public ArrayList MediaVideos { get; set; }
        public string ImdbURL { get; set; }

        private string GoogleSearch = "http://www.google.com/search?q=imdb+";
        private string BingSearch = "http://www.bing.com/search?q=imdb+";
        private string AskSearch = "http://www.ask.com/web?q=imdb+";

        private string wIMDbtitle = "http://www.imdb.com/title/";
        #endregion public & private definitions

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="MovieName">file:// or http://www.imdb.com/title/tt, parse directly. Returns (ref) ImdbURL </param>
        /// <param name="refineSearch">If not empty, show a Dialog for refine. If false, return first result. </param>
        public IMDb(ProgressBar pbSplash, ref string MovieName, string refineSearch = "", bool bMP = false, bool mostrarAvisos = false)
        {
            if (!((MovieName.StartsWith("file:")) || (MovieName.StartsWith(wIMDbtitle + "tt"))))
            {
                string html = getHtmlFromEngine(System.Uri.EscapeUriString(MovieName));
                if(!string.IsNullOrEmpty(html))
                    MovieName = getIMDbUrl(html, MovieName, refineSearch, bMP, mostrarAvisos);
            }
            pbSplash.Increment(30);
            /*activando este bloque sólo busca ImdbURL, sin parsear la página en IMDb
            if (bMP)
                return;
             */
            if (!string.IsNullOrEmpty(MovieName))
                parseIMDbPage(MovieName);
            pbSplash.Increment(30);
        }

        internal string getHtmlFromEngine(string MovieName, string searchEngine = "google")
        {
            if (string.IsNullOrEmpty(MovieName))
                return string.Empty;
            APIFilmAffinityIMDb.Functions f = new APIFilmAffinityIMDb.Functions();
            string url = GoogleSearch + MovieName;
            if (searchEngine.ToLower().Equals("bing"))
                url = BingSearch + MovieName;
            if (searchEngine.ToLower().Equals("ask"))
                url = AskSearch + MovieName;
            string html = f.getDataWebR(url);

            if (!string.IsNullOrEmpty(html))
                return html;
            else if (searchEngine.ToLower().Equals("google"))
                return getHtmlFromEngine(MovieName, "bing");
            else if (searchEngine.ToLower().Equals("bing"))
                return getHtmlFromEngine(MovieName, "ask");
            else //search fails
                return string.Empty;


        }

        internal string getIMDbUrl(string html, string MovieName, string refineSearch, bool bMP = false, bool mostrarAvisos = false, string searchEngine = "google")
        {
            APIFilmAffinityIMDb.Functions f = new APIFilmAffinityIMDb.Functions();
            int nMaxIDMDb = 8;//recupera sólo los nMaxIDMDb primeros, posteriormente distinctMatches extrae los distintos; es una forma de optimizar la búsqueda
            var distinctMatches = f.matchAll(@"/(tt\d{7})/", html, 1, nMaxIDMDb).OfType<System.String>().Select(x => x).Distinct();//sólo busca @"/(tt\d{7})/ quito wIMDbtitle a ver que tal, por problema con Matrix reloaded
            List<string> imdbUrls = new List<string>();
            foreach (string item in distinctMatches)
                imdbUrls.Add(item);
            string imdbUrl = string.Empty;
            if (imdbUrls.Count > 0)
            {
                imdbUrl = imdbUrls[0].ToString();
                if (!string.IsNullOrEmpty(refineSearch) && (!bMP))
                {
                    List<string[]> ListMovies = new List<string[]>();
                    foreach (string item in imdbUrls)
                        ListMovies.Add(new string[] { wIMDbtitle + item + "/", item.Replace("tt", string.Empty) });
                    APIFilmAffinityIMDb.dlgSelector dlgSelector = new APIFilmAffinityIMDb.dlgSelector(MovieName + " (" + refineSearch.ToString() + ")", ListMovies, MovieName, typeof(IMDb_Scraper.IMDb), mostrarAvisos);
                    string idIDMDb = dlgSelector.idFA;
                    dlgSelector.Dispose();
                    dlgSelector = null;
                    if (string.IsNullOrEmpty(idIDMDb))
                        return string.Empty;
                    imdbUrl = "tt" + idIDMDb;
                }
                return wIMDbtitle + imdbUrl + "/"; //return first IMDb result
            }
            if (mostrarAvisos)
                MessageBox.Show("No se encontró resultado en IMDb para " + System.Uri.UnescapeDataString(MovieName) + Environment.NewLine + refineSearch, Application.ProductName, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            return string.Empty;
        }

        internal void parseIMDbPage(string imdbUrlPage)
        {
            APIFilmAffinityIMDb.Functions f = new APIFilmAffinityIMDb.Functions();
            string html = string.Empty;
            if (!imdbUrlPage.StartsWith("file:"))
                imdbUrlPage = imdbUrlPage + "combined";
            html = f.getDataWebR(imdbUrlPage, true);
            Id = f.match(@"<link rel=""canonical"" href=""" + wIMDbtitle + @"(tt\d{7})/combined"" />", html);
            if (!string.IsNullOrEmpty(Id))
            {
                status = true;
                ArrayList am = f.matchAll(@"src=""(.*?)._V1", f.match(@"<td><div class=""media_strip_thumbs"">(.*?)</div></td>", html));
                MediaImages = string.Join(Environment.NewLine, (string[])am.ToArray(Type.GetType("System.String")));
                MediaVideos = f.matchAll(@"href=""/video(.*?)<noscript><img", f.match(@"<tr><td><div class=""media_strip_thumbs"">(.*?)</div></td></tr>", html, 1));
                ArrayList listVideos = new ArrayList();
                foreach (string rowVideos in MediaVideos)
                {
                    string sLink = f.match(@"/(.*?)/""", rowVideos);
                    string sImg = f.match(@"loadlate=""(.*?)._V1_", rowVideos);
                    string sThumb = f.match(@"loadlate=""(.*?)""", rowVideos);
                    string sName = f.match(@"_ZA(.*?),", sThumb);
                    sName = Regex.Replace(sName, @"(%\d{4})", " ");//"Full%2520Episode"
                    if (string.IsNullOrEmpty(sName))
                        sName = "Vídeo " + (listVideos.Count + 1).ToString();
                    listVideos.Add(new string[4] { sName, sImg, sThumb, "http://www.imdb.com/video/" + sLink });
                }
                MediaVideos = listVideos;
                ArrayList arrCast = f.matchAll(@"<tr class=(.*?)</tr>", f.match(@"<h3>Cast</h3>(.*?)</table>", html));
                ArrayList listCast = new ArrayList();
                foreach (string rowCast in arrCast)
                {
                    string sLink = f.match(@"/name/(nm\d{7})/", rowCast);
                    string sImg = string.Empty;
                    string sImgJPG = f.match(@"<img src=""(.*?)""", rowCast);
                    string sName = f.match(@"<td class=""nm""><a.*?href=""/name/.*?/"".*?>(.*?)</a>", rowCast);
                    ArrayList listAs = new ArrayList();
                    listAs = f.matchAll(@"<a.*?href=""/character/.*?/"".*?>(.*?)</a>", rowCast);
                    string sAs = f.match(@"<td class=""char""><a.*?href=""/character/.*?/"".*?>(.*?)</a>", rowCast);
                    if (listAs.Count > 0)
                        sAs = string.Join(" / ", (string[])listAs.ToArray(Type.GetType("System.String")));
                    if (string.IsNullOrEmpty(sAs))
                        sAs = f.match(@"<td class=""char"">(.*?)</td>", rowCast);
                    int iPos = sImgJPG.IndexOf("._V1");
                    if (iPos > -1)
                        //http://ia.media-imdb.com/images/M/MV5BODI2MTk5NjIxOV5BMl5BanBnXkFtZTcwMTYzMDc0Mw@@._V1._SX23_SY30_.jpg
                        sImg = sImgJPG.Substring(0, iPos);
                    listCast.Add(new string[4] { sName, sAs, sImg, "http://www.imdb.com/name/" + sLink + "/" });
                }
                FullCast = listCast;
                Title = f.match(@"<title>(IMDb \- )*(.*?) \(.*?</title>", html, 2);
                OriginalTitle = f.match(@"title-extra"">(.*?)<", html);
                if (html.IndexOf("(????)") < 0)//infinite loop for IMBd no year TODO add time span to regex
                    Year = f.match(@"<title>.*?\(.*?(\d{4}).*?\).*?</title>", html);
                Rating = f.match(@"<b>(\d.\d)/10</b>", html);
                Genres = f.matchAll(@"<a.*?>(.*?)</a>", f.match(@"Genre.?:(.*?)(</div>|See more)", html));
                Directors = f.matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", f.match(@"Directed by</a></h5>(.*?)</table>", html));
                Writers = f.matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", f.match(@"Writing credits</a></h5>(.*?)</table>", html));
                Producers = f.matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", f.match(@"Produced by</a></h5>(.*?)</table>", html));
                Musicians = f.matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", f.match(@"Original Music by</a></h5>(.*?)</table>", html));
                Cinematographers = f.matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", f.match(@"Cinematography by</a></h5>(.*?)</table>", html));
                Editors = f.matchAll(@"<td valign=""top""><a.*?href=""/name/.*?/"">(.*?)</a>", f.match(@"Film Editing by</a></h5>(.*?)</table>", html));
                namesCast = f.matchAll(@"<td class=""nm""><a.*?href=""/name/.*?/"".*?>(.*?)</a>", f.match(@"<h3>Cast</h3>(.*?)</table>", html));
                Plot = f.match(@"Plot:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", html);
                PlotKeywords = f.matchAll(@"<a.*?>(.*?)</a>", f.match(@"Plot Keywords:</h5>.*?<div class=""info-content"">(.*?)</div", html));
                ReleaseDate = f.match(@"Release Date:</h5>.*?<div class=""info-content"">.*?(\d{1,2} (January|February|March|April|May|June|July|August|September|October|November|December) (19|20)\d{2})", html);
                Runtime = f.match(@"Runtime:</h5><div class=""info-content"">(\d{1,4}) min[\s]*.*?</div>", html);
                Top250 = f.match(@"Top 250: #(\d{1,3})<", html);
                Oscars = f.match(@"Won (\d+) Oscars?\.", html);
                if (string.IsNullOrEmpty(Oscars) && "Won Oscar.".Equals(f.match(@"(Won Oscar\.)", html))) Oscars = "1";
                Awards = f.match(@"(\d{1,4}) wins", html);
                Nominations = f.match(@"(\d{1,4}) nominations", html);
                Tagline = f.match(@"Tagline:</h5>.*?<div class=""info-content"">(.*?)(<a|</div)", html);
                MpaaRating = f.match(@"MPAA</a>:</h5><div class=""info-content"">Rated (G|PG|PG-13|PG-14|R|NC-17|X) ", html);
                Votes = f.match(@">(\d+,?\d*) votes<", html);
                Languages = f.matchAll(@"<a.*?>(.*?)</a>", f.match(@"Language.?:(.*?)(</div>|>.?and )", html));
                Countries = f.matchAll(@"<a.*?>(.*?)</a>", f.match(@"Country:(.*?)(</div>|>.?and )", html));
                Poster = f.match(@"<div class=""photo"">.*?<a name=""poster"".*?><img.*?src=""(.*?)"".*?</div>", html);
                if (!string.IsNullOrEmpty(Poster) && Poster.IndexOf("media-imdb.com") > 0)
                    Poster = Regex.Replace(Poster, @"._V1.*?.jpg", string.Empty);
                ImdbURL = wIMDbtitle + Id + "/";
            }
        }
    }
}
