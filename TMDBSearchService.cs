using System.Net.Http;
using System.Collections.Generic;
using System;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class TMDBSearchService{
     
     //Example: https://api.themoviedb.org/3/search/movie?api_key=dee675e4fea7d68fd4220eab97a497eb&language=en-US&query=Fight%20Club&page=1&include_adult=false
    private static readonly HttpClient client = new HttpClient();
    private static readonly WebClient downloadClient = new WebClient();

    private const string TMDBSearchAddress = "https://api.themoviedb.org/3/search/movie";
    private const string TMDBPosterAddress = "http://image.tmdb.org/t/p";
    private const string PosterSize = @"/w185";
    private const string TMDBAPIKey = "dee675e4fea7d68fd4220eab97a497eb";
   
    public TMDBSearchService(){
        client.BaseAddress = new Uri(TMDBSearchAddress);
           
    }

    private static async Task<String> EncodeURLParamters(Dictionary<string, string> UrlParameters){
        using (HttpContent content = new FormUrlEncodedContent(UrlParameters)){
            return await content.ReadAsStringAsync();
        }
    }

    public static async Task<SearchRoot> ConductSearch(string movieTitle){

        string URLEncodedQuery = await EncodeURLParamters(getUrlParams(movieTitle));
        using (HttpResponseMessage response = await client.GetAsync(TMDBSearchAddress + "?" + URLEncodedQuery, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();
            string responseText = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<SearchRoot>(responseText); 
        }
    }

    public static void downloadPoster(String posterURLName, String posterLocation, String posterFileName){                      
            //OR @"c:\temp\image35.png" +@"\poster\"
            Console.WriteLine(TMDBPosterAddress + PosterSize + posterURLName);
            posterLocation += @"\poster\";
             if(!Directory.Exists(posterLocation)){
                new System.IO.FileInfo(posterLocation).Directory.Create();
                downloadClient.DownloadFile(new Uri(TMDBPosterAddress + PosterSize + posterURLName), posterLocation + posterFileName);            
            }               
    }

    private static Dictionary<string, string> getUrlParams(string searchTerm){
        Dictionary<string, string> urlParameters = new Dictionary<string, string>();
         //Assign URL paramter defaults.
        urlParameters.Add("api_key", TMDBAPIKey);
        urlParameters.Add("language", "en-US");
        urlParameters.Add("page", "1");
        urlParameters.Add("include_adult", "false");
        urlParameters.Add("query", searchTerm);
        return urlParameters;
    }

    private static TMDBSearchService instance = new TMDBSearchService();
}