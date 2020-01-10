namespace WebBloatScore.Models
{
    public sealed class CaptureResult
    {
        public string Page { get; set; }
        public string PageTitle { get; set; }
        public long PageSize { get; set; } // the total size of all resources
        public int PageCount { get; set; } // the total count of resources
        public string PageDetails => "/Details/" + this.DetailsPath;
        public string Image { get; set; } // the screenshot of the whole page
        public long ImageSize { get; set; } // the size of the screenshot of the whole page
        public double BS { get { return this.PageSize / (double)this.ImageSize; } }
        public ExitCode Exit { get; set; }

        public string DetailsPath { get; set; }
        public string ImagePath { get; set; }
    }
}