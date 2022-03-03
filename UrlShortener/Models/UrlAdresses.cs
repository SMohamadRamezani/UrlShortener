using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ShortUrl.Models
{
    public class UrlAdresses
    {
        public int Id { get; set; }
        public string ShortUrl { get; set; }
        public DateTime CreatDate { get; set; } = DateTime.Now;
        public int CreatedBy { get; set; }
        public string RealUrl { get; set; }
    }
}