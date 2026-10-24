using MudBlazor;

namespace Simulab.Web.Components.Ui;

/// <summary>
/// The only place that names library icons (rule: ui-project). Family: Material Outlined.
/// Pages use these semantic names; the same action has the same icon everywhere.
/// </summary>
public static class AppIcons
{
    public const string Add = Icons.Material.Outlined.Add;
    public const string Edit = Icons.Material.Outlined.Edit;
    public const string Delete = Icons.Material.Outlined.Delete;
    public const string Search = Icons.Material.Outlined.Search;
    public const string More = Icons.Material.Outlined.MoreVert;
    public const string Retry = Icons.Material.Outlined.Refresh;
    public const string Restart = Icons.Material.Outlined.RestartAlt;
    public const string Close = Icons.Material.Outlined.Close;
    public const string History = Icons.Material.Outlined.History;
    public const string Back = Icons.Material.Outlined.ArrowBack;
    public const string Save = Icons.Material.Outlined.Save;
    public const string Warning = Icons.Material.Outlined.WarningAmber;
    public const string Error = Icons.Material.Outlined.ErrorOutline;
    public const string Empty = Icons.Material.Outlined.Inbox;

    /// <summary>F-134: a page that was not found (also what a user sees on a route the user may not open).</summary>
    public const string NotFound = Icons.Material.Outlined.SearchOff;

    /// <summary>F-134: the menu sections on the home page, one icon each.</summary>
    public const string SectionStudy = Icons.Material.Outlined.School;
    public const string SectionContent = Icons.Material.Outlined.LibraryBooks;
    public const string SectionAdministration = Icons.Material.Outlined.AdminPanelSettings;
    public const string SectionDevelopment = Icons.Material.Outlined.Build;
    public const string LightMode = Icons.Material.Outlined.LightMode;
    public const string DarkMode = Icons.Material.Outlined.DarkMode;
    public const string Language = Icons.Material.Outlined.Language;
    public const string View = Icons.Material.Outlined.Visibility;
    public const string Copy = Icons.Material.Outlined.ContentCopy;
    public const string Download = Icons.Material.Outlined.Download;
    public const string Upload = Icons.Material.Outlined.UploadFile;
    public const string Archive = Icons.Material.Outlined.Archive;
    public const string Menu = Icons.Material.Outlined.Menu;
    public const string Home = Icons.Material.Outlined.Home;
    public const string ChevronLeft = Icons.Material.Outlined.ChevronLeft;
    public const string ChevronRight = Icons.Material.Outlined.ChevronRight;
    public const string Build = Icons.Material.Outlined.Build;
    public const string Hide = Icons.Material.Outlined.VisibilityOff;
    public const string Mail = Icons.Material.Outlined.MailOutline;
    public const string Verified = Icons.Material.Outlined.CheckCircle;
    public const string Expired = Icons.Material.Outlined.HistoryToggleOff;
    public const string Account = Icons.Material.Outlined.AccountCircle;
    public const string SignOut = Icons.Material.Outlined.Logout;
    public const string Password = Icons.Material.Outlined.Password;
    public const string Roles = Icons.Material.Outlined.AdminPanelSettings;
    public const string Users = Icons.Material.Outlined.Group;

    /// <summary>F-33: the organizers of the assessment catalog.</summary>
    public const string Organizers = Icons.Material.Outlined.AccountBalance;

    /// <summary>F-34 v2: the bodies that publish a notice.</summary>
    public const string IssuingAuthorities = Icons.Material.Outlined.Gavel;

    /// <summary>F-34: the exams of the assessment catalog.</summary>
    public const string Exams = Icons.Material.Outlined.Description;

    /// <summary>F-79: the subjects of the taxonomy.</summary>
    public const string Subjects = Icons.Material.Outlined.MenuBook;

    /// <summary>F-79: the topics under a subject.</summary>
    public const string Topics = Icons.Material.Outlined.Bookmarks;

    /// <summary>F-90: what uses a subject or a topic (the row action that explains a disabled delete).</summary>
    public const string WhereUsed = Icons.Material.Outlined.AccountTree;
    public const string Security = Icons.Material.Outlined.Security;

    /// <summary>F-41: the AI gateway, and sending one prompt to it.</summary>
    public const string Ai = Icons.Material.Outlined.AutoAwesome;
    public const string Send = Icons.Material.Outlined.Send;

    /// <summary>F-43: what kind of assessment an exam is.</summary>
    public const string AssessmentType = Icons.Material.Outlined.Category;

    /// <summary>F-43: how far an exam reaches.</summary>
    public const string Scope = Icons.Material.Outlined.Public;

    /// <summary>F-43: a state or a municipality — where something applies.</summary>
    public const string Place = Icons.Material.Outlined.Place;

    /// <summary>F-43: an item of a checklist that is still missing.</summary>
    public const string Pending = Icons.Material.Outlined.RadioButtonUnchecked;

    /// <summary>F-35: the editions of an exam, each one a paper actually applied.</summary>
    public const string Editions = Icons.Material.Outlined.EventNote;

    /// <summary>F-35: the notice (edital) an edition was cut from.</summary>
    public const string Notice = Icons.Material.Outlined.Article;

    /// <summary>F-35: an address the reader can follow.</summary>
    public const string Link = Icons.Material.Outlined.Link;

    /// <summary>F-103: a picture in a rich text.</summary>
    public const string Image = Icons.Material.Outlined.Image;

    /// <summary>F-35: the calendar button of the date field.</summary>
    public const string Calendar = Icons.Material.Outlined.CalendarToday;

    /// <summary>F-36: the student catalog, where a student searches the published exams.</summary>
    public const string Catalog = Icons.Material.Outlined.ManageSearch;

    /// <summary>F-36: puts every filter of a list back to "All".</summary>
    public const string ClearFilters = Icons.Material.Outlined.FilterAltOff;

    /// <summary>F-36: a link that opens another site in a new tab.</summary>
    public const string OpenInNew = Icons.Material.Outlined.OpenInNew;

    /// <summary>F-74: the subjects of an edition, as its notice lists them.</summary>
    public const string NoticeSubjects = Icons.Material.Outlined.ListAlt;

    /// <summary>F-74: moves a row of a sortable list up.</summary>
    public const string MoveUp = Icons.Material.Outlined.ArrowUpward;

    /// <summary>F-74: moves a row of a sortable list down.</summary>
    public const string MoveDown = Icons.Material.Outlined.ArrowDownward;

    /// <summary>F-114: the arrow at the end of a list field, closed. The library default is the Filled family.</summary>
    public const string Expand = Icons.Material.Outlined.ArrowDropDown;

    /// <summary>F-114: the same arrow while the list is open.</summary>
    public const string Collapse = Icons.Material.Outlined.ArrowDropUp;

    /// <summary>F-95: the question bank, its menu item and the statement section.</summary>
    public const string Questions = Icons.Material.Outlined.Quiz;

    /// <summary>F-95: the base text a question refers to.</summary>
    public const string BaseText = Icons.Material.Outlined.Notes;

    /// <summary>F-95: the options of a question, one of them the key.</summary>
    public const string Options = Icons.Material.Outlined.RadioButtonChecked;

    /// <summary>F-104: a question with several correct options.</summary>
    public const string MultipleAnswer = Icons.Material.Outlined.Checklist;

    /// <summary>F-106: a question of two columns whose items are matched.</summary>
    public const string Matching = Icons.Material.Outlined.CompareArrows;

    /// <summary>F-107: a question whose statement has blanks the student types in; its type card and key section.</summary>
    public const string FillInTheBlanks = Icons.Material.Outlined.ShortText;

    /// <summary>F-107: the toolbar button that inserts a blank marker at the caret.</summary>
    public const string Blank = Icons.Material.Outlined.SpaceBar;

    /// <summary>F-95: why the key is right.</summary>
    public const string Explanation = Icons.Material.Outlined.Info;

    /// <summary>F-95: makes a draft available to the bank.</summary>
    public const string Publish = Icons.Material.Outlined.Publish;

    /// <summary>F-95: takes a published question back to a draft.</summary>
    public const string Unpublish = Icons.Material.Outlined.Unpublished;

    /// <summary>F-105: the review queue and its row action, and the review history card.</summary>
    public const string Review = Icons.Material.Outlined.FactCheck;

    /// <summary>F-105: takes a question back out of review.</summary>
    public const string Withdraw = Icons.Material.Outlined.Undo;

    /// <summary>F-105: a reviewer approves a question, which publishes it.</summary>
    public const string Approve = Icons.Material.Outlined.TaskAlt;

    /// <summary>F-105: a reviewer rejects a question with a reason.</summary>
    public const string Reject = Icons.Material.Outlined.Cancel;

    /// <summary>F-95: the rich text field's toolbar.</summary>
    public const string FormatBold = Icons.Material.Outlined.FormatBold;
    public const string FormatItalic = Icons.Material.Outlined.FormatItalic;
    public const string FormatUnderline = Icons.Material.Outlined.FormatUnderlined;
    public const string FormatStrikethrough = Icons.Material.Outlined.FormatStrikethrough;
    public const string Superscript = Icons.Material.Outlined.Superscript;
    public const string Subscript = Icons.Material.Outlined.Subscript;
    public const string BulletList = Icons.Material.Outlined.FormatListBulleted;
    public const string NumberedList = Icons.Material.Outlined.FormatListNumbered;
    public const string Quote = Icons.Material.Outlined.FormatQuote;
    public const string ClearFormatting = Icons.Material.Outlined.FormatClear;
}
