﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WebStudio.Enums;
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

        [HttpGet]
        public IActionResult DetailCard(string cardId)
        {
            if (cardId != null)
            {
                Card card = _db.Cards.FirstOrDefault(c => c.Id == cardId);
                if (card != null)
                {
                    return View(card);
                }

                return NotFound();
            }

            return NotFound();
        }

        [HttpGet]
        public async Task<IActionResult> DeleteCard(string cardId)
        {
            if (cardId != null)
            {
                Card card = _db.Cards.FirstOrDefault(c => c.Id == cardId);
                if (card != null)
                {
                    card.CardState = CardState.Удалена;
                    _db.Cards.Update(card);
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Index", "Cards");
        }
        
        // [HttpGet]
        // public async Task<IActionResult> DeleteCardAjax(string cardId)
        // {
        //     if (cardId != null)
        //     {
        //         Card card = _db.Cards.FirstOrDefault(c => c.Id == cardId);
        //         if (card != null)
        //         {
        //             card.CardState = CardState.Удалена;
        //             _db.Cards.Update(card);
        //             await _db.SaveChangesAsync();
        //         }
        //     }
        //
        //     return Json(new {success = true, text = "Карточка удалена"});
        // }
    }
}