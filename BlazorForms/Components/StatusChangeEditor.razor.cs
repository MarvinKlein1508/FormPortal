using BlazorForms.Core;
using BlazorForms.Core.Constants;
using BlazorForms.Core.Extensions;
using BlazorForms.Core.Models;
using DbController;
using DbController.MySql;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MimeKit;

namespace BlazorForms.Components
{
    public partial class StatusChangeEditor
    {
        [Parameter, EditorRequired]
        public FormEntry? Entry { get; set; }
        [Parameter, EditorRequired]
        public User? User { get; set; }
        [Parameter, EditorRequired]
        public EventCallback<FormEntryStatusChange> OnSaved { get; set; }
        [Parameter, EditorRequired]
        public EventCallback OnCancel { get; set; }

        public FormEntryStatusChange? Input { get; set; }
        private EditForm? _form;

        private List<User> _availableForNotification = [];
        private bool _allowStatusChange;
        protected override async Task OnInitializedAsync()
        {
            ArgumentNullException.ThrowIfNull(User, nameof(User));
            ArgumentNullException.ThrowIfNull(Entry, nameof(Entry));

            _availableForNotification = await SetupNotifiersAsync(Entry, User);

            Input = new()
            {
                UserId = User.UserId,
                EntryId = Entry.EntryId,
                StatusId = Entry.StatusId,
            };

            foreach (var user in _availableForNotification)
            {
                Input.Notifiers.Add(new FormEntryHistoryNotify
                {
                    UserId = user.UserId,
                    Notify = user.StatusChangeNotificationDefault
                });
            }

            // Check if current user is allowed to change status
            _allowStatusChange = await CheckPermissionsAsync();
        }

        private async Task<bool> CheckPermissionsAsync()
        {
            if (Entry is null || User is null)
            {
                return false;
            }

            bool isAdmin = await authService.HasRole(Roles.EDIT_ENTRIES);

            // Admins are always allowed to make changes
            if (isAdmin)
            {
                return true;
            }

            var searchStatus = Storage.Get<FormStatus, int?>(Entry.StatusId);
            bool isCompleted = searchStatus?.IsCompleted ?? false;

            // Completed form entries can only be changed by admins
            if (isCompleted)
            {
                return false;
            }

            var user = Entry.Form.ManagerUsers.FirstOrDefault(x => x.UserId == User.UserId);
            bool isManager = user is not null;
            bool isAllowedToApprove = user?.CanApprove ?? false;
            bool requiresApproval = searchStatus?.RequiresApproval ?? false;

            return (requiresApproval && isAllowedToApprove) || (isManager && !requiresApproval);
        }

        private async Task<List<User>> SetupNotifiersAsync(FormEntry entry, User currentUser)
        {
            List<User> result = [];
            List<int> userIds = [];

            // Generate list of available notifiers
            foreach (var user in entry.Form.ManagerUsers)
            {
                // We don't want to notify ourself
                if (user.UserId == currentUser.UserId)
                {
                    continue;
                }

                result.Add(user);
                userIds.Add(user.UserId);
            }

            if (entry.CreationUserId is int creationUserId && currentUser.UserId != creationUserId && !userIds.Contains(creationUserId))
            {
                using IDbController dbController = new MySqlController();
                User? creationUser = await userService.GetAsync((int)entry.CreationUserId, dbController);
                if (creationUser is not null)
                {
                    result.Add(creationUser);
                    userIds.Add(creationUserId);
                }
            }


            return result;
        }

        private async Task SaveAsync()
        {
            if (Input is null || _form is null || _form.EditContext is null || Entry is null)
            {
                return;
            }

            using IDbController dbController = new MySqlController();
            await dbController.StartTransactionAsync();
            Input.DateAdded = DateTime.Now;

            if (_form.EditContext.Validate())
            {
                try
                {
                    if (Input.HistoryId is 0)
                    {
                        await statusChangeService.CreateAsync(Input, dbController);
                    }
                    else
                    {
                        await statusChangeService.UpdateAsync(Input, dbController);
                    }

                    // Update the status of the entry
                    // Check if old status needed approve
                    var status = Storage.Get<FormStatus, int?>(Entry.StatusId);
                    if (status is not null && status.RequiresApproval)
                    {
                        // We only approve once, even when the second status requires another approval
                        await statusChangeService.ApproveAsync(Entry.EntryId, dbController);
                        Entry.IsApproved = true;

                    }
                    Entry.StatusId = Input.StatusId;
                    await dbController.CommitChangesAsync();
                }
                catch (Exception)
                {
                    await dbController.RollbackChangesAsync();
                    throw;
                }


                // Send E-Mails
                if (emailSettings.Value.Enabled && Input.Notifiers.Any(x => x.Notify))
                {
                    List<string> email_addresses = [];

                    foreach (var notify in Input.Notifiers.Where(x => x.Notify))
                    {
                        var user = _availableForNotification.First(x => x.UserId == notify.UserId);
                        email_addresses.Add(user.Email);
                    }

                    if (email_addresses.Count != 0)
                    {
                        await Input.SendMailForEntryStatusChangeAsync(email_addresses, Entry, navigationManager.BaseUri, emailSettings.Value);
                    }
                }

                await JSRuntime.ShowToastAsync(ToastType.success, AppLocalizer["SAVE_MESSAGE"]);
                await OnSaved.InvokeAsync(Input);
            }
        }
    }
}