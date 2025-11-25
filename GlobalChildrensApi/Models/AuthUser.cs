using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GlobalChildrensApi.Models
{
    public class AuthUser
    {
        public Guid Id { get; set; }
        public Guid? Instance_Id { get; set; }
        public string? Aud { get; set; }
        public string? Role { get; set; }
        public string? Email { get; set; }
        public string? Encrypted_Password { get; set; }
        public DateTimeOffset? Email_Confirmed_At { get; set; }
        public DateTimeOffset? Invited_At { get; set; }
        public string? Confirmation_Token { get; set; }
        public DateTimeOffset? Confirmation_Sent_At { get; set; }
        public string? Recovery_Token { get; set; }
        public DateTimeOffset? Recovery_Sent_At { get; set; }
        public string? Email_Change_Token_New { get; set; }
        public string? Email_Change { get; set; }
        public DateTimeOffset? Email_Change_Sent_At { get; set; }
        public DateTimeOffset? Last_Sign_In_At { get; set; }
        public string? Raw_App_Meta_Data { get; set; }
        public string? Raw_User_Meta_Data { get; set; }
        public bool? Is_Super_Admin { get; set; }
        public DateTimeOffset? Created_At { get; set; }
        public DateTimeOffset? Updated_At { get; set; }
        public string? Phone { get; set; }
        public DateTimeOffset? Phone_Confirmed_At { get; set; }
        public string? Phone_Change { get; set; }
        public string? Phone_Change_Token { get; set; }
        public DateTimeOffset? Phone_Change_Sent_At { get; set; }
        public DateTimeOffset? Confirmed_At { get; set; }
        public string? Email_Change_Token_Current { get; set; }
        public short? Email_Change_Confirm_Status { get; set; }
        public DateTimeOffset? Banned_Until { get; set; }
        public string? Reauthentication_Token { get; set; }
        public DateTimeOffset? Reauthentication_Sent_At { get; set; }
        public bool Is_Sso_User { get; set; }
        public DateTimeOffset? Deleted_At { get; set; }
        public bool Is_Anonymous { get; set; }
    }
}
