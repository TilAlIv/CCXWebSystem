﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebStudio.Enums;
using WebStudio.Models;
using WebStudio.ViewModels;
using X.PagedList;

namespace WebStudio.Controllers
{
    public class CardsController : Controller
    {
        private WebStudioContext _db;
        private UserManager<User> _userManager;
        IWebHostEnvironment _appEnvironment;
        

        public CardsController(WebStudioContext db, UserManager<User> userManager, IWebHostEnvironment appEnvironment)
        {
            _db = db;
            _userManager = userManager;
            _appEnvironment = appEnvironment;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(_db.Cards.ToList());
        }

        /// <summary>
        /// Детальная информация на карточке
        /// </summary>
        /// <param name="cardId">для поиска в базе карт метод принимает её Id</param>
        /// <returns></returns>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult> DetailCard2(string cardId)
        {
            if (cardId != null)
            {
                DetailCardViewModel model = new DetailCardViewModel
                {
                    Card =  _db.Cards.FirstOrDefault(c => c.Id == cardId),
                    CardId = cardId,
                    Users =  _db.Users.ToList(),
                    FileModels = _db.Files.Where(f => f.CardId == cardId).ToList()
                };

                return View(model);
            }

            return NotFound();
        }

        /// <summary>
        /// Данный Action помечает карту удалённой
        /// </summary>
        /// <param name="cardId">для поиска в базе карт на удаление карты метод принимает её Id</param>
        /// <returns></returns>
        [HttpPost]
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
                    return RedirectToAction("AllCardsList", "Cards", new {sort = CardState.Новая});
                }
                return NotFound();
            }

            return NotFound();
        }

        /// <summary>
        /// Данный Action позволяет восстановить карту в новое состояние.
        /// </summary>
        /// <param name="cardId">Id карты по которому ищется карта БД.</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> RestoreCard(string cardId)
        {
            if (cardId != null)
            {
                Card card = _db.Cards.FirstOrDefault(c => c.Id == cardId);
                if (card != null)
                {
                    card.CardState = CardState.Новая;
                    _db.Cards.Update(card);
                    await _db.SaveChangesAsync();
                }
            }

            return RedirectToAction("AllCardsList", "Cards", new {sort = CardState.Новая});
        }
        
        /// <summary>
        /// Данный Action передает карту ответственному исполнителю и меняет её статус.
        /// </summary>
        /// <param name="model">Данный параметр является передаваемым контейнером для сущности Card.</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> TakeCard(DetailCardViewModel model)
        {
            if (model.CardId != null)
            {
                Card card = _db.Cards.FirstOrDefault(c => c.Id == model.CardId);
                if (card != null)
                {
                    card.CardState = CardState.Проработка;
                    card.ExecutorId = model.UserId;
                    card.Executor = await _userManager.FindByIdAsync(model.UserId);
                    card.DateOfProcessingEnd = model.Card.DateOfProcessingEnd;
                    card.DateOfAuctionStartUpdated = model.Card.DateOfAuctionStartUpdated;
                    
                    _db.Cards.Update(card);
                    await _db.SaveChangesAsync();
                }
            }
            return RedirectToAction("AllCardsList", "Cards", new {sort = CardState.Проработка});
        }

        
       [HttpPost]
        [Authorize]
        public async Task<IActionResult> ChangeCardStatus(DetailCardViewModel model, string cardState, int bid)
        {
            if (model.CardId != null)
            {
                Card card = _db.Cards.FirstOrDefault(c => c.Id == model.CardId);
                if (card != null)
                {
                    switch (cardState)
                    {
                        case "ПКО": card.CardState = CardState.ПКО; 
                            card.DateOfProcessingEnd = model.Card.DateOfProcessingEnd;
                            card.DateOfAuctionStartUpdated = model.Card.DateOfAuctionStartUpdated; 
                            break;
                        
                        case "Торги": card.CardState = CardState.Торги; 
                            card.DateOfProcessingEnd = model.Card.DateOfProcessingEnd;
                            card.DateOfAuctionStartUpdated = model.Card.DateOfAuctionStartUpdated;
                            break;
                        
                        case "Удалена": card.CardState = CardState.Удалена; break;
                        case "Проиграна": card.CardState = CardState.Проиграна; card.Bidding = bid;
                            SaveCloneCard(card); break;
                        
                        case "Выиграна": card.CardState = CardState.Выиграна; SaveCloneCard(card); break;
                        case "Активна": card.CardState = CardState.Активна; break;
                        case "Закрыта": card.CardState = CardState.Закрыта; break;
                    }
                    _db.Cards.Update(card);
                    await _db.SaveChangesAsync();
                    return RedirectToAction("DetailCard2", "Cards", new {cardId = card.Id});
                }
            }
            return NotFound("Не найдено");
        }

        [NonAction]
        private Card ChangeDateTimesOnDetailsCard(DateTime processDateTime, DateTime auctionStartDateTimeUpdate, Card card)
        {
            if (card != null && processDateTime != DateTime.MinValue && auctionStartDateTimeUpdate != DateTime.MinValue)
            {
                card.DateOfProcessingEnd = card.DateOfProcessingEnd != processDateTime
                    ? processDateTime
                    : card.DateOfProcessingEnd;
                card.DateOfAuctionStartUpdated = card.DateOfAuctionStartUpdated != auctionStartDateTimeUpdate
                    ? auctionStartDateTimeUpdate : card.DateOfAuctionStartUpdated;
            }

            return new Card();
        }
        
        [HttpPost]
        public async Task<IActionResult> AddFile(IFormFileCollection uploads, string cardId)
        {
            if (!string.IsNullOrEmpty(cardId))
            {
                Card card = _db.Cards.FirstOrDefault(c => c.Id == cardId);
                if (card != null)
                {
                    foreach(var uploadedFile in uploads)
                    {
                        if (!Directory.Exists( _appEnvironment.WebRootPath + $"/Files/{card.Number}"))
                        {
                            Directory.CreateDirectory(_appEnvironment.WebRootPath + $"/Files/{card.Number}");
                        }
                        
                        string path = $"/Files/{card.Number}" + uploadedFile.FileName;
                        using (var fileStream = new FileStream(_appEnvironment.WebRootPath + path, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(fileStream);
                        }
                        FileModel file = new FileModel { Name = uploadedFile.FileName, Path = path, CardId = card.Id, Card = card};
                        _db.Files.Add(file); 
                    } 
                    _db.SaveChanges(); 
                    return RedirectToAction("DetailCard2", "Cards", new {cardId = cardId});
                }
                return Content("Такой карточки не обнаруженно");
                 
            }

            return NotFound();
        }
        
       /// <summary>
        /// Данный Action позволяет делать отображение, фильтрацию карт и пагинацию страниц по статусам, датам и исполнителям.
        /// </summary>
        /// <param name="page">Номер страницы</param>
        /// <param name="from">С какого числа</param>
        /// <param name="to">По какое число</param>
        /// <param name="filter">Выбор фильтрации</param>
        /// <param name="sort">Сортировка статусов карт</param>
        /// <returns></returns>
       [HttpGet]
       [Authorize]
       public IActionResult AllCardsList(int? page, DateTime? from, DateTime? to, string filter, CardState sort)
        {
            List<Card> cards = new List<Card>();
            
            switch (sort)
            {
                case CardState.Новая:
                    cards = _db.Cards.Where(c => c.CardState == CardState.Новая).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.Новая;
                    break;
                
                case CardState.Удалена: 
                    cards = _db.Cards.Where(c => c.CardState == CardState.Удалена).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.Удалена;
                    break;
                
                case CardState.Проработка: 
                    cards = _db.Cards.Where(c => c.CardState == CardState.Проработка).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.Проработка;
                    break;
                
                case CardState.ПКО: 
                    cards = _db.Cards.Where(c => c.CardState == CardState.ПКО).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.ПКО;
                    break;
                
                case CardState.Торги: 
                    cards = _db.Cards.Where(c => c.CardState == CardState.Торги).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.Торги;
                    break;
                
                case CardState.Выиграна: 
                    cards = _db.Cards.Where(c => c.CardState == CardState.Выиграна).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.Выиграна;
                    break;
                
                case CardState.Проиграна: 
                    cards = _db.Cards.Where(c => c.CardState == CardState.Проиграна).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.Проиграна;
                    break;
                
                case CardState.Активна: 
                    cards = _db.Cards.Where(c => c.CardState == CardState.Активна).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.Активна;
                    break;
                
                case CardState.Закрыта: 
                    cards = _db.Cards.Where(c => c.CardState == CardState.Закрыта).OrderByDescending(c=>c.Number).ToList();
                    ViewBag.sort = CardState.Закрыта;
                    break;
            }

            if (filter != null)
            {
                switch (filter)
                {
                    case "DateOfAcceptingEnd":
                        if (from != null || to != null)
                        {
                            cards = cards.Where(c => c.DateOfAcceptingEnd >= from && c.DateOfAcceptingEnd <= to).ToList();
                        }
                        break;

                    case "DateOfAuctionStart":
                        if (from != null || to != null)
                        {
                            cards = cards.Where(c => c.DateOfAuctionStart >= from && c.DateOfAuctionStart <= to).ToList();
                        }
                        break;
                    
                    default:
                        cards = cards.Where(c => c.ExecutorId == filter).ToList();
                        ViewBag.filter = filter;
                        break;
                }
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            return View(cards.ToPagedList(pageNumber, pageSize));
        }
       
       [HttpGet]
       [Authorize]
       public IActionResult GetCardInfoHistory(int? page, DateTime? from, DateTime? to, string filter, CardState sort)
        {
            List<CardClone> cards = new List<CardClone>();
            
            switch (sort)
            {
                case CardState.Участвовашие: 
                    cards = _db.HistoryOfVictoryAndLosing.Where(c => c.CardState == CardState.Выиграна || c.CardState == CardState.Проиграна).ToList();
                    ViewBag.sort = CardState.Участвовашие;
                    break;
            }

            if (filter != null)
            {
                switch (filter)
                {
                    case "DateOfAcceptingEnd":
                        if (from != null || to != null)
                        {
                            cards = cards.Where(c => c.DateOfAcceptingEnd >= from && c.DateOfAcceptingEnd <= to).ToList();
                        }
                        break;

                    case "DateOfAuctionStart":
                        if (from != null || to != null)
                        {
                            cards = cards.Where(c => c.DateOfAuctionStart >= from && c.DateOfAuctionStart <= to).ToList();
                        }
                        break;
                    
                    default:
                        cards = cards.Where(c => c.ExecutorId == filter).ToList();
                        ViewBag.filter = filter;
                        break;
                }
            }

            int pageSize = 20;
            int pageNumber = page ?? 1;

            return View(cards.ToPagedList(pageNumber, pageSize));
        }

       [HttpGet]
       [Authorize]
       public IActionResult AuctionCards(AuctionCardsViewModel model, string filterOrder, string executorName, int? page)
       {
           model.Cards = _db.HistoryOfVictoryAndLosing.ToList();
           switch (filterOrder)
           {
               case "All":
                   model.Cards = model.Cards;
                   ViewBag.filterOrder = filterOrder;
                   break;
               case "Number":
                   model.Cards = model.Cards.OrderBy(c => c.Number).ToList();
                   ViewBag.filterOrder = filterOrder;
                   break;
               case "CardSummDesc":
                   model.Cards = model.Cards.OrderByDescending(c => c.StartSumm).ToList();
                   ViewBag.filterOrder = filterOrder;
                   break;
               case "CardSummAsc":
                   model.Cards = model.Cards.OrderBy(c => c.StartSumm).ToList();
                   ViewBag.filterOrder = filterOrder;
                   break;
           }

           if (executorName != null)
           {
               model.Cards = model.Cards.Where(c => c.Executor.Name.Contains(model.ExecutorName) 
                                                    || c.Executor.Surname.Contains(model.ExecutorName)).ToList();
               ViewBag.executorName = executorName;
           }
           int pageSize = 20;
           int pageNumber = (page ?? 1);
           return View(model.Cards.ToPagedList(pageNumber, pageSize));
       }

       [NonAction]
       private void SaveCloneCard(Card card)
       {
           CardClone cardClone = new CardClone()
           {
               Auction = card.Auction,
               Bidding = card.Bidding,
               Broker = card.Broker,
               Executor = card.Executor,
               Initiator = card.Initiator,
               Links = card.Links,
               Name = card.Name,
               Number = card.Number,
               Positions = card.Positions,
               State = card.State,
               BestPrice = card.BestPrice,
               CardState = card.CardState,
               ExecutorId = card.ExecutorId,
               LinkNames = card.LinkNames,
               StartSumm = card.StartSumm,
               DateOfAcceptingEnd = card.DateOfAcceptingEnd,
               DateOfAuctionStart = card.DateOfAuctionStart
           };
           _db.HistoryOfVictoryAndLosing.Add(cardClone);
           _db.SaveChanges();

       }
       

       [HttpPost]
       [Authorize]
       public async Task<IActionResult> Comment(DetailCardViewModel model)
       {
           if (model.Comment.Message != null)
           {
               Comment comment = new Comment
               {
                   Message = model.Comment.Message,
                   DateOfSend = DateTime.Now,
                   CardId = model.Comment.CardId,
                   Card = _db.Cards.FirstOrDefault(c => c.Id == model.Comment.CardId),
                   UserId = model.Comment.UserId,
                   User = await _userManager.FindByIdAsync(model.Comment.UserId)
               };
               await _db.Comments.AddAsync(comment);
               await _db.SaveChangesAsync();
           }
           
           return RedirectToAction("DetailCard2", "Cards", new {cardId = model.Comment.CardId});
       }

       [HttpGet]
       [Authorize(Roles = "admin")]
       public async Task<IActionResult> ChangeComment(string commentId)
       {
           if (commentId != null)
           {
               Comment comment = _db.Comments.FirstOrDefault(c => c.Id == commentId);
               if (comment != null)
               {
                   ChangeCommentViewModel model = new ChangeCommentViewModel
                   {
                       Id = comment.Id,
                       Message = comment.Message,
                   };
                   return View(model);
               }

               return NotFound();
           }

           return NotFound();
       }

       [HttpPost]
       public async Task<IActionResult> ChangeComment(ChangeCommentViewModel model)
       {
           if (ModelState.IsValid)
           {
               Comment comment = _db.Comments.FirstOrDefault(c => c.Id == model.Id);
               if (comment != null)
               {
                   comment.Message = model.Message;
                   comment.DateOfChange = DateTime.Now;
                   
                   _db.Comments.Update(comment);
                   await _db.SaveChangesAsync();
                   return RedirectToAction("DetailCard2", "Cards", new {cardId = comment.Card.Id});
               }

               return NotFound();
           }

           return View(model);
       }
    }
}