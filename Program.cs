using System;
using Nest;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GregFlixIndexer
{
    class Program
    {

        public static List<string> stopwordsList = new List<string>() {"[bonkai77] ",
                                                        "BR",
                                                       // "Br",
                                                        "WEB",
                                                        "rip",
                                                        "Rip",
                                                        "srt",
                                                        "mkv",
                                                        "mp4",
                                                        "[Prof] ",
                                                        "[daijoubu] ",
                                                        "[1080p]",
                                                        "x265",
                                                        "[DUAL - AUDIO]",
                                                        "[x265]",
                                                        "x264",
                                                        "V2",
                                                        "[HEVC]",
                                                        "BOKUTOX",
                                                        "XVID",
                                                        "(ENHANCED)",
                                                        "[AAC]",
                                                        "[10bit]",
                                                        "(2012)",
                                                        "(2019)",
                                                        "[WEBRip]",
                                                        "[DVD]",
                                                        "[DvD]",
                                                        "YIFY",
                                                        "[YTS.AM]",
                                                        "(1984)_[720p,BluRay,x264]_-_THORA",
                                                        "[YTS.AG]",
                                                        "(Eng Dub)",
                                                        "(2001)",
                                                        "(2015)",
                                                        "(2017)",
                                                        "(2016)",
                                                        "1-64",
                                                        "1080p",
                                                        "[YTS.PE]",
                                                        "(1988)",
                                                        "[Dual Audio5.1]",
                                                        "[x265HEVC]",
                                                        "[{bk]",
                                                        "(1080p Bluray x265 HEVC 10bit AAC 5.1 Tigole)",
                                                        "(1992)",
                                                        "(2005-)",
                                                        "(1984)",
                                                        "1988",
                                                        "2001",
                                                        "2008",
                                                        "2012",
                                                        "2015",
                                                        "2016",
                                                        "2017",
                                                        "2018",
                                                        "2019",
                                                        "DVDScr",
                                                        "AC3",
                                                        "HQ",
                                                        "Hive-CM8",
                                                        "_-_THORA",
                                                        " S01-S09 ",
                                                        "[DVD H264 AAC]",
                                                        "[720p]",
                                                        "720p",
                                                        "Blu",
                                                        "Ray",
                                                        "[]",
                                                        ";",
                                                        " (360p re-webrip)",
                                                        " (360p re-blurip)",
                                                        "(_FIRST_TRY)",
                                                        "__[720p,BluRay,x264]",
                                                        "VPPV",
                                                        " + Movies",
                                                        "[DUAL-AUDIO]",
                                                        "-",
                                                        "( Bluray  HEVC 10bit AAC 5.1 Tigole)",
                                                        "   {bk",
                                                        "   HEVC 10bit BDRip Dual Audio AAC",                                                        
                                                        "  ",
                                                        "   ",
                                                        "     ",
                                                        };

        public static ElasticClient elasticClient;
        public static TMDBSearchService movieSearchClient;
        public static int mediaIndex = 0;     

        static async Task Main(string[] args)
        {                   
            //http://localhost:9200/media_index/_search?pretty=true&q=*:*  <--This will return all records for our index.
            var node = new Uri("http://localhost:9200");
            var settings = new ConnectionSettings(node)
                .DefaultIndex("media_index");
                settings.BasicAuthentication("nope", "nope");         
            elasticClient = new ElasticClient(settings);
            movieSearchClient = new TMDBSearchService();
            DirectoryInfo root = new DirectoryInfo(@"E:\Rentts");
            Console.WriteLine("Please provide input for what you would like to do?[<sanitize>,<test>,<search>]  NOTE:  Neither of these operations are recoverable " +
                              "after they have been performed.  The file paths will be sanatized of any entry in the stopword list and " +
                              "Elasticsearch indexes will have to be dropped and re-written if you need an update.");

            string inputCommand = Console.ReadLine();

             switch(inputCommand){
                        case "sanitize":
                           if(elasticClient.Indices.Exists("media_index").Exists){
                                elasticClient.Indices.Delete("media_index");
                           }                                  
                           walkDirectoryTree(root);
                        break;
                        case "test":
                            elasticAutoComplete("Office");
                        break;
                        case "search":
                        SearchRoot searchResults = await TMDBSearchService.ConductSearch("Fight Club");
                        foreach(Result result in searchResults.results){
                            Console.WriteLine(JsonConvert.SerializeObject(result));
                            Console.WriteLine("--NEW MOVIE--");
                        }
                        break;
                        case "delete":
                            elasticClient.Indices.Delete("media_index");
                        break;
                        default:
                        Console.WriteLine("Why even are you?");
                        break;
                    }  
        }

        public static void testElasticWrites(){
                      

            var media = new Media
            {
                id = 1,
                Season = 1,
                Episode = 1,
                Name = "Kardashian Season 1",
                Location = @"media\Kardashians\S1\E1.mp4",
                posterLocation = @"poster\location\post.jpg",
                Description = "Ughhh"
            };

            //var response = client.IndexAsync(tweet, idx => idx.Index("mytweetindex")); // returns a Task<IndexResponse>

            // Or, in an async-context
            //var response = await client.IndexAsync(tweet, idx => idx.Index("mytweetindex")); // awaits a Task<IndexResponse>
        }

        public static void walkDirectoryTree(DirectoryInfo root) {

            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;

            try{
                files = root.GetFiles("*.*");
            }
            catch (UnauthorizedAccessException e){
                Console.WriteLine("Something about access restrictions.  There is likely a way to elevate in framework.");
            }
            catch (DirectoryNotFoundException e){
                Console.WriteLine(e.Message);
            }

            if (files != null){
               
                subDirs = root.GetDirectories();
                
                foreach (DirectoryInfo dirInfo in subDirs){

                    string[] baseDirectories = dirInfo.FullName.Split("\\");

                    string mediaType = baseDirectories[baseDirectories.Length-1];                     
                    
                    switch(mediaType){

                        case "movies":                                        
                            var movieSubDirectories = dirInfo.GetDirectories();
                             
                            foreach(DirectoryInfo dirs in movieSubDirectories ){
                                writeMovieTitles(dirs);
                            }                            
                        break;

                        case "series":
                            //var seriesSubDirectories = dirInfo.GetDirectories();                        
                            
                            //foreach(DirectoryInfo dirs in seriesSubDirectories ){                                
                                //writeSeriesTitles(dirs);
                            //}
                        break;
                        default:
                        Console.WriteLine("Break at the books directory");
                        break;
                    }                    

                    //Console.WriteLine(dirInfo.FullName);                  
                    //Directory.Move();
                    string sanitizedDirectory = dirInfo.FullName;
                    foreach (string sub in stopwordsList) {
                       sanitizedDirectory = sanitizedDirectory.Replace(sub, "");
                    }
                    sanitizedDirectory = sanitizedDirectory.Replace(" ", "_");
                    Console.WriteLine(sanitizedDirectory);
            
                    //var newDirectory =  new DirectoryInfo();
                    //string someText = "this is some text just some dummy text Just text";
                    //List<string> stopwordsList = new List<string>() { "some", "just", "text" };
                    //Console.WriteLine(string.Join(" ", dirInfo.FullName.Split().Where(w => !stopwordsList.Contains(w))));
                    //someText = ); ;

                    //walkDirectoryTree(dirInfo);
                }
            }
        }

        public static void writeMovieTitles(DirectoryInfo mediaRoot){
              
              //Console.WriteLine("What is Media Root? --");
              //Console.WriteLine(mediaRoot.FullName);              

              var mediaContainerFormats = new List<string>{"AAC", "MP3", "MP4", "WAV", "WebM", "mkv", "avi"};

              List<FileInfo> files = GetFilesByExtension(mediaRoot.FullName, ".jpg", ".txt");
              
              foreach(FileInfo file in files){
                   file.Attributes = FileAttributes.Normal;                   
                   File.Delete(file.FullName);
              }
              FileInfo[] mediaFiles = mediaRoot.GetFiles();

              if(mediaFiles.Length == 0){
                  Console.WriteLine("Path Being Removed: ");                  
                  Console.WriteLine(mediaRoot.FullName);
                  Directory.Delete(mediaRoot.FullName);
              }else{
                foreach(FileInfo file in mediaRoot.GetFiles()){
                  sanatizeFilePaths(file);
                  //Directory.Delete(file.FullName);                                                        
                }
              }                                            
        }

        //This enforces a rather strict adherence to an implied directory structure.  Can be updated to work better with recusrion.
        public static void writeSeriesTitles(DirectoryInfo mediaRoot){

            var seasons = mediaRoot.GetDirectories();
            var mediaFiles = mediaRoot.GetFiles();

            if(seasons.Length == 0 && mediaFiles.Length == 0){
                  Console.WriteLine("Path Being Removed: ");                  
                  Console.WriteLine(mediaRoot.FullName);
                  Directory.Delete(mediaRoot.FullName);
            }else{
                if(seasons == null || seasons.Length == 0){      
                int episodeNumber = 1;                                     
                foreach(FileInfo episode in  mediaRoot.GetFiles()){
                    sanatizeFilePaths(episode, 1, episodeNumber);
                    episodeNumber++;
                }
            }else{
                int seasonNumber = 1;                 
                foreach(DirectoryInfo season in seasons){                   
                    var seasonEpisodes = season.GetFiles();
                    int episodeNumber = 1;
                    foreach (FileInfo episode in seasonEpisodes){
                        sanatizeFilePaths(episode, seasonNumber, episodeNumber);
                        episodeNumber++;
                    }
                    seasonNumber++;
                }
            }
            }                        
        }

        public static void sanatizeFilePaths(FileInfo file, int season = 0, int episode = 0){

            string sanitizedDirectory;
            string sanatizedFileName = file.Name;
            string sanatizedFilePath = file.DirectoryName;
             string mediaExtension = file.Extension;
            if(!string.IsNullOrEmpty(file.Extension)){
                mediaExtension = file.Extension;
            }else{
                string reextendedPath = file.FullName.Insert(file.FullName.Length - 3,".");
                Console.WriteLine("file.FullName: " + file.FullName);
                Console.WriteLine("reextendedPath" + reextendedPath);
                System.IO.File.Move(file.FullName, reextendedPath);
                FileInfo extensionedFile = new FileInfo(reextendedPath);
                Console.WriteLine("FileInfo Extension" + extensionedFile.Extension);
                sanatizeFilePaths(extensionedFile, season, episode);
                return;
            }
            
            sanatizedFileName = sanatizedFileName.Replace(mediaExtension, "");

            foreach (string sub in stopwordsList) {
                sanatizedFilePath = sanatizedFilePath.Replace(sub, "");
                sanatizedFileName = sanatizedFileName.Replace(sub, "");
            }

            sanatizedFilePath = sanatizedFilePath.Replace(" ", "_");
            sanatizedFilePath = sanatizedFilePath.Replace(".", "");
            sanatizedFilePath = sanatizedFilePath.TrimEnd('_');

            sanatizedFileName = sanatizedFileName.Replace(" ", "_");
            sanatizedFileName = sanatizedFileName.TrimEnd('_');
            sanatizedFileName += mediaExtension;
            
            sanitizedDirectory = sanatizedFilePath + @"\" + sanatizedFileName;

            Console.WriteLine("sanitizedDirectory " + sanitizedDirectory);

            if(!Directory.Exists(sanitizedDirectory)){
                new System.IO.FileInfo(sanitizedDirectory).Directory.Create();
                File.Move(file.FullName, sanitizedDirectory);
            }

            string oldPath = Path.GetDirectoryName(file.FullName);
            string[] allFiles = Directory.GetFiles(oldPath);                        
                                    
            FileInfo movieFile = new FileInfo(sanitizedDirectory);
            string rootPath = movieFile.Directory.Root.ToString();
            string accessPath = movieFile.Directory.FullName.Replace(rootPath, "");                       

            string searchTerm = movieFile.Name.Replace(mediaExtension, "").Replace("_", " ");
            Console.WriteLine("searchTerm: " + searchTerm);
            var task =Task.Run(() => TMDBSearchService.ConductSearch(searchTerm));
            task.Wait();
            SearchRoot response = task.Result;
            if(response.results.Any()){
                Result movie;
                
                Console.WriteLine("sanitizedDirectory: " + sanitizedDirectory);
                
                bool endLoop = true;
                int resultsIndex = 0;
                do{
                    movie = response.results[resultsIndex];
                    Console.WriteLine(JsonConvert.SerializeObject(movie));
                    string cycleCommand = Console.ReadLine();
                    switch(cycleCommand){ 
                        case "a":
                            TMDBSearchService.downloadPoster(movie.poster_path, sanatizedFilePath, movieFile.Name.Replace(mediaExtension, "") + ".jpg");
                            endLoop = false;
                        break;
                        case "n":
                            resultsIndex++;
                        break;
                        default:
                            endLoop = false;
                        break;
                    }
                }while(endLoop);
                var media = new Media{
                    id = mediaIndex++,
                    tmbd_id = movie.id,
                    Name = movie.original_title,
                    Location = accessPath,
                    Description = movie.overview,
                    posterLocation = "",
                    Genre = movie.genre_ids
                };

                if(season >= 1){
                    media.Season = season;
                    media.Episode = episode;
                }
            }
            
            //Task<SearchRoot> searchResultsTask = ;
            //searchResultsTask.Wait();
            //SearchRoot searchResults = searchResultsTask.Result;
            //Console.WriteLine(JsonConvert.SerializeObject(searchResults.results[0]));
            
            //foreach(Result result in searchResults.results){
            //    Console.WriteLine(JsonConvert.SerializeObject(result));
            //    Console.WriteLine("--NEW MOVIE--");
            //}
                        
            Console.WriteLine(sanitizedDirectory);
            //elsaticMediaWrite(media);

        }

        public static void elasticAutoComplete(string partialTerm) {
            try {
                //var autoCompleteQuery = Query<Media>.QueryString(q => q
                //.Query("*" + partialTerm + "*")
                //.DefaultField(p => p.Name));

                var response = elasticClient.Search<Media>(s => s
                .Explain()
                .From(0)
                .Size(10)
                .Query(q => q.QueryString(qs => qs
                    .Query("*" + partialTerm + "*")
                    .DefaultField(p=>p.Name))));

                Console.WriteLine();
                Console.WriteLine(response.Documents.Count);

                var autoCompleteResults = response.Documents.ToList<Media>();

                foreach (Media m in autoCompleteResults) {
                    Console.WriteLine(m.Name);
                    Console.WriteLine("Season: " + m.Season);
                    Console.WriteLine("Episode: " + m.Episode);
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine("I have an exception in the auto complete");
            }
        }

        public static void elsaticMediaWrite(Media media){

              try {
                var response = elasticClient.Index(media, idx => idx.Index("media_index")); //or specify index via settings.DefaultIndex("mytweetindex");
                Console.WriteLine("Trying to write: " + media.Name);                                
                var idxResponse = elasticClient.IndexDocument(media);
                Console.WriteLine(idxResponse.Result.ToString());               
            }
            catch(Exception ex){
                Console.WriteLine(ex.Message);
                Console.WriteLine("I have an exception");
            }

        }

        public static void elasticMediaReads(){
            
            try{
                var fetchResponse = elasticClient.Get<Media>(1, idx => idx.Index("media_index")); // returns an IGetResponse mapped 1-to-1 with the Elasticsearch JSON response
                //var me = fetchResponse.Source; // the original document
                Console.WriteLine(fetchResponse.Source.Description);
                Console.WriteLine("I am still in the try block");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("I have an exception from the get");
            }
        }

        public static List<FileInfo> GetFilesByExtension(string path, params string[] extensions){
            List<FileInfo> list = new List<FileInfo>();
            foreach (string ext in extensions)
                list.AddRange(new DirectoryInfo(path).GetFiles("*" + ext).Where(p =>
                    p.Extension.Equals(ext,StringComparison.CurrentCultureIgnoreCase))
                    .ToArray());
                return list;
        }
    }

    public class Media {
        public int id { get; set; }
        public int tmbd_id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }
        public string posterLocation { get; set; }
        public string Type { get; set; }
        public bool watched { get; set;}
        public int Season { get; set; }
        public int Episode { get; set; }
        public List<int> Genre { get; set; }
    }
}
