﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Virtual_School_Register.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Title cannot be empty!")]
        public string Title { get; set; }

        //[DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "Content cannot be empty!")]
        public string Content { get; set; }

        [Display(Name = "Recipient")]
        public string RecipientId { get; set; }

        public bool IsSenderDeleted { get; set; }

        public bool IsRecipientDeleted { get; set; }

        public string UserId { get; set; }

        public virtual User User { get; set; }
    }
}
