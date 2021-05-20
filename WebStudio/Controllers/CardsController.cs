﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using WebStudio.Models;
using X.PagedList;

namespace WebStudio.Controllers
{
    public class CardsController : Controller
    {
        private WebStudioContext _db;

        public CardsController(WebStudioContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Index(int? page)
        {
            List<Card> cards = _db.Cards.ToList();
            int pageSize = 20;
            int pageNumber = page ?? 1;

            return View(cards.ToPagedList(pageNumber, pageSize));
        }
    }
}