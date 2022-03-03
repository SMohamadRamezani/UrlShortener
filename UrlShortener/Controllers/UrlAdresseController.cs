using PagedList;
using ShortUrl.DAL;
using ShortUrl.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace ShortUrl.Controllers
{
    public class UrlAdresseController : Controller
    {
        private ShortUrlContext dbShortUrl = new ShortUrlContext();
        private static string shorturlCharsLcase = "abcdefgijkmnopqrstwxyz";
        private static string shorturlCharsUcase = "ABCDEFGHJKLMNPQRSTWXYZ";
        private static string shorturlCharsNumeric = "23456789";

        public ViewResult Index(string sortOrder, string currentFilter, string searchString, int? page)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "CreateDate" : "";
            ViewBag.DateSortParm = sortOrder == "Date" ? "CreateDate" : "Date";

            if (searchString != null)
            {
                page = 1;
            }
            else
            {
                searchString = currentFilter;
            }

            ViewBag.CurrentFilter = searchString;

            var ShortUrl = from s in dbShortUrl.UrlAdresses
                           select s;
            if (!String.IsNullOrEmpty(searchString))
            {
                ShortUrl = ShortUrl.Where(s => s.ShortUrl.Contains(searchString)
                                       || s.RealUrl.Contains(searchString));
            }
            switch (sortOrder)
            {
                case "ShortUrl":
                    ShortUrl = ShortUrl.OrderByDescending(s => s.ShortUrl);
                    break;
                case "CreatDate":
                    ShortUrl = ShortUrl.OrderBy(s => s.CreatDate);
                    break;
                case "RealUrl":
                    ShortUrl = ShortUrl.OrderByDescending(s => s.RealUrl);
                    break;
                default:  // Name ascending 
                    ShortUrl = ShortUrl.OrderBy(s => s.ShortUrl);
                    break;
            }

            int pageSize = 3;
            int pageNumber = page ?? 1;
            return View(ShortUrl.ToPagedList(pageNumber, pageSize));
        }
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = " RealUrl")] UrlAdresses urlAdresse)
        {
            try
            {
                var strShortUrl = GenerateShortUrl(urlAdresse.RealUrl);
                if (ModelState.IsValid)
                {
                    urlAdresse.CreatDate = DateTime.Now;
                    urlAdresse.ShortUrl = strShortUrl;
                    dbShortUrl.UrlAdresses.Add(urlAdresse);
                    dbShortUrl.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (RetryLimitExceededException)
            {
                
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            return View(urlAdresse);
        }
        public string GenerateShortUrl(string realUrl)
        {
            string ShortUrl = RandomCharacters();
            return ShortUrl;
        }
        public static string RandomCharacters()
        {
            // Create a local array containing supported short-url characters
            // grouped by types.
            char[][] charGroups = new char[][]
            {
                shorturlCharsLcase.ToCharArray(),
                shorturlCharsUcase.ToCharArray(),
                shorturlCharsNumeric.ToCharArray()
            };

            // Use this array to track the number of unused characters in each
            // character group.
            int[] charsLeftInGroup = new int[charGroups.Length];

            // Initially, all characters in each group are not used.
            for (int i = 0; i < charsLeftInGroup.Length; i++)
                charsLeftInGroup[i] = charGroups[i].Length;

            // Use this array to track (iterate through) unused character groups.
            int[] leftGroupsOrder = new int[charGroups.Length];

            // Initially, all character groups are not used.
            for (int i = 0; i < leftGroupsOrder.Length; i++)
                leftGroupsOrder[i] = i;

            // Because we cannot use the default randomizer, which is based on the
            // current time (it will produce the same "random" number within a
            // second), we will use a random number generator to seed the
            // randomizer.

            // Use a 4-byte array to fill it with random bytes and convert it then
            // to an integer value.
            byte[] randomBytes = new byte[4];

            // Generate 4 random bytes.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            rng.GetBytes(randomBytes);

            // Convert 4 bytes into a 32-bit integer value.
            int seed = (randomBytes[0] & 0x7f) << 24 |
                        randomBytes[1] << 16 |
                        randomBytes[2] << 8 |
                        randomBytes[3];

            // Now, this is real randomization.
            Random random = new Random(seed);

            // This array will hold short-url characters.
            char[] shortUrl = null;

            // Allocate appropriate memory for the short-url.
            shortUrl = new char[random.Next(5, 5)];

            // Index of the next character to be added to short-url.
            int nextCharIdx;

            // Index of the next character group to be processed.
            int nextGroupIdx;

            // Index which will be used to track not processed character groups.
            int nextLeftGroupsOrderIdx;

            // Index of the last non-processed character in a group.
            int lastCharIdx;

            // Index of the last non-processed group.
            int lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;

            // Generate short-url characters one at a time.
            for (int i = 0; i < shortUrl.Length; i++)
            {
                // If only one character group remained unprocessed, process it;
                // otherwise, pick a random character group from the unprocessed
                // group list. To allow a special character to appear in the
                // first position, increment the second parameter of the Next
                // function call by one, i.e. lastLeftGroupsOrderIdx + 1.
                if (lastLeftGroupsOrderIdx == 0)
                    nextLeftGroupsOrderIdx = 0;
                else
                    nextLeftGroupsOrderIdx = random.Next(0,
                                                         lastLeftGroupsOrderIdx);

                // Get the actual index of the character group, from which we will
                // pick the next character.
                nextGroupIdx = leftGroupsOrder[nextLeftGroupsOrderIdx];

                // Get the index of the last unprocessed characters in this group.
                lastCharIdx = charsLeftInGroup[nextGroupIdx] - 1;

                // If only one unprocessed character is left, pick it; otherwise,
                // get a random character from the unused character list.
                if (lastCharIdx == 0)
                    nextCharIdx = 0;
                else
                    nextCharIdx = random.Next(0, lastCharIdx + 1);

                // Add this character to the short-url.
                shortUrl[i] = charGroups[nextGroupIdx][nextCharIdx];

                // If we processed the last character in this group, start over.
                if (lastCharIdx == 0)
                    charsLeftInGroup[nextGroupIdx] =
                                              charGroups[nextGroupIdx].Length;
                // There are more unprocessed characters left.
                else
                {
                    // Swap processed character with the last unprocessed character
                    // so that we don't pick it until we process all characters in
                    // this group.
                    if (lastCharIdx != nextCharIdx)
                    {
                        char temp = charGroups[nextGroupIdx][lastCharIdx];
                        charGroups[nextGroupIdx][lastCharIdx] =
                                    charGroups[nextGroupIdx][nextCharIdx];
                        charGroups[nextGroupIdx][nextCharIdx] = temp;
                    }
                    // Decrement the number of unprocessed characters in
                    // this group.
                    charsLeftInGroup[nextGroupIdx]--;
                }

                // If we processed the last group, start all over.
                if (lastLeftGroupsOrderIdx == 0)
                    lastLeftGroupsOrderIdx = leftGroupsOrder.Length - 1;
                // There are more unprocessed groups left.
                else
                {
                    // Swap processed group with the last unprocessed group
                    // so that we don't pick it until we process all groups.
                    if (lastLeftGroupsOrderIdx != nextLeftGroupsOrderIdx)
                    {
                        int temp = leftGroupsOrder[lastLeftGroupsOrderIdx];
                        leftGroupsOrder[lastLeftGroupsOrderIdx] =
                                    leftGroupsOrder[nextLeftGroupsOrderIdx];
                        leftGroupsOrder[nextLeftGroupsOrderIdx] = temp;
                    }
                    // Decrement the number of unprocessed groups.
                    lastLeftGroupsOrderIdx--;
                }
            }

            // Convert password characters into a string and return the result.
            return new string(shortUrl);
        }
        public ActionResult Delete(int? id, bool? saveChangesError = false)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            if (saveChangesError.GetValueOrDefault())
            {
                ViewBag.ErrorMessage = "Delete failed. Try again, and if the problem persists see your system administrator.";
            }
            UrlAdresses oShortUrl = dbShortUrl.UrlAdresses.Find(id);
            if (oShortUrl == null)
            {
                return HttpNotFound();
            }
            return View(oShortUrl);
        }

        // POST: urlAdresse/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                UrlAdresses oShortUrl = dbShortUrl.UrlAdresses.Find(id);
                dbShortUrl.UrlAdresses.Remove(oShortUrl);
                dbShortUrl.SaveChanges();
            }
            catch (RetryLimitExceededException)
            {
                
                return RedirectToAction("Delete", new { id = id, saveChangesError = true });
            }
            return RedirectToAction("Index");
        }
        public ActionResult Redirect(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            UrlAdresses oUrlAdresses = dbShortUrl.UrlAdresses.Find(id);
            if (oUrlAdresses == null)
            {
                return HttpNotFound();
            }
            return Redirect(oUrlAdresses.RealUrl);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbShortUrl.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}