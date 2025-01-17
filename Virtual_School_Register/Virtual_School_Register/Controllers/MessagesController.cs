﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Virtual_School_Register.Data;
using Virtual_School_Register.EmailConfig;
using Virtual_School_Register.Models;

namespace Virtual_School_Register.Controllers
{
    [Authorize(Roles = "Admin, Nauczyciel, Rodzic, Uczen")]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender _emailSender;

        public MessagesController(ApplicationDbContext context, UserManager<User> userManager, IEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        // GET: Messages
        public async Task<IActionResult> Index()
        {
            var messages = await _context.Message.Include(u => u.User)
                .Where(x => (x.RecipientId == _userManager.GetUserId(HttpContext.User)) && x.IsRecipientDeleted == false)
                .OrderBy(x => x.Date).Reverse().ToListAsync();

            ViewBag.InboxMessage = "Received";
            ViewBag.UserType = "Sender";

            return View(messages);
        }

        public async Task<IActionResult> IndexSent()
        {
            var messages = await _context.Message.Include(u => u.User)
                .Where(x => (x.UserId == _userManager.GetUserId(HttpContext.User)) && x.IsSenderDeleted == false)
                .OrderBy(x => x.Date).Reverse().ToListAsync();

            var users = await _userManager.Users.ToListAsync();

            foreach (var m in messages)
            {
                if (m.RecipientId != null)
                {
                    var recipient = users.Find(x => x.Id == m.RecipientId);
                    m.RecipientId = recipient.Name + " " + recipient.Surname;
                }
            }

            ViewBag.InboxMessage = "Sent";
            ViewBag.UserType = "Recipient";

            return View("Index", messages);
        }

        // GET: Messages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Message.Include(m => m.User).FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        // GET: Messages/Create
        public IActionResult Create(string userId, string title)
        {
            List<User> persons;

            if (userId == null)
            {
                if (User.IsInRole("Rodzic") || User.IsInRole("Uczen"))
                {
                    persons = _userManager.Users.Where(x => x.Type == "Nauczyciel" && x.Id != _userManager.GetUserId(HttpContext.User))
                        .OrderBy(x => x.Type).ThenBy(x => x.Surname).ThenBy(x => x.Name).ToList();
                }
                else
                {
                    persons = _userManager.Users.Where(x => x.Id != _userManager.GetUserId(HttpContext.User))
                        .OrderBy(x => x.Type).ThenBy(x => x.Surname).ThenBy(x => x.Name).ToList();
                }
            }
            else
            {
                persons = _userManager.Users.Where(x => x.Id == userId).ToList();

                ViewBag.IsReply = "Reply";
                ViewBag.ReplyTitle = "Re: " + title;
            }

            ViewBag.PersonsList = persons;

            //if (User.IsInRole("Rodzic") || User.IsInRole("Uczen"))
            //{
            //    persons = _userManager.Users.Where(x => x.Type == "Nauczyciel" && x.Id != _userManager.GetUserId(HttpContext.User))
            //        .OrderBy(x => x.Type).ThenBy(x => x.Surname).ThenBy(x => x.Name).ToList();
            //}
            //else if (User.IsInRole("Nauczyciel"))
            //{
            //    persons = _userManager.Users.Where(x => (x.Type == "Rodzic" || x.Type == "Uczen") && x.Id != _userManager.GetUserId(HttpContext.User))
            //        .OrderBy(x => x.Type).ThenBy(x => x.Surname).ThenBy(x => x.Name).ToList();
            //}
            //else
            //{
            //    persons = _userManager.Users.Where(x => x.Id != _userManager.GetUserId(HttpContext.User))
            //        .OrderBy(x => x.Type).ThenBy(x => x.Surname).ThenBy(x => x.Name).ToList();
            //}

            //ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Messages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Message message)
        {
            if (ModelState.IsValid)
            {
                message.UserId = _userManager.GetUserId(HttpContext.User);
                message.Date = DateTime.Now;

                _context.Add(message);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", message.UserId);
            return View(message);
        }

        [Authorize(Roles = "Admin, Nauczyciel")]
        public IActionResult CreateEmailToClassParents()
        {
            /*var classes = _context.Class.Where(x => x.ClassTutorId == _userManager.GetUserId(HttpContext.User))
                .OrderBy(x => x.Name).ToList();*/

            var classes = _context.Class.OrderBy(x => x.Name).ToList();

            ViewBag.ClassesList = classes;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmailToClassParents(Message message)
        {
            if (ModelState.IsValid)
            {
                message.UserId = _userManager.GetUserId(HttpContext.User);
                message.Date = DateTime.Now;

                var classStudents = _userManager.Users.Where(x => x.ClassId.ToString() == message.RecipientId).ToList();

                List<Message> messagesList = new List<Message>();

                foreach (var student in classStudents)
                {
                    if(student.ParentId != null)
                    {
                        var parentEmail = _userManager.Users.FirstOrDefault(x => x.Id == student.ParentId).Email;

                        Message newMessage = new Message();

                        newMessage.Title = message.Title;
                        newMessage.Date = message.Date;
                        newMessage.Content = message.Content;
                        newMessage.RecipientId = student.ParentId;
                        newMessage.UserId = message.UserId;

                        var emailMessage = new MyMessage(new string[] { parentEmail }, newMessage.Title, newMessage.Content);
                        _emailSender.SendEmail(emailMessage);

                        messagesList.Add(newMessage);
                    }
                }

                _context.Message.AddRange(messagesList);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", message.UserId);
            return View(message);
        }

        // GET: Messages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Message.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", message.UserId);
            return View(message);
        }

        // POST: Messages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MessageId,Title,Date,Content,RecipientId,UserId")] Message message)
        {
            if (id != message.MessageId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(message);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MessageExists(message.MessageId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", message.UserId);
            return View(message);
        }

        // GET: Messages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var message = await _context.Message.Include(m => m.User).FirstOrDefaultAsync(m => m.MessageId == id);

            if (message == null)
            {
                return NotFound();
            }

            return View(message);
        }

        // POST: Messages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool indexFlag = true;
            var message = await _context.Message.FindAsync(id);

            if (message.RecipientId == _userManager.GetUserId(HttpContext.User))
            {
                message.IsRecipientDeleted = true;
                indexFlag = true;
            }
            else if (message.UserId == _userManager.GetUserId(HttpContext.User))
            {
                message.IsSenderDeleted = true;
                indexFlag = false;
            }

            if (message.IsSenderDeleted && message.IsRecipientDeleted)
            {
                _context.Message.Remove(message);
            }

            await _context.SaveChangesAsync();

            if (!indexFlag)
            {
                return RedirectToAction(nameof(IndexSent));
            }
            return RedirectToAction(nameof(Index));
        }

        private bool MessageExists(int id)
        {
            return _context.Message.Any(e => e.MessageId == id);
        }
    }
}
