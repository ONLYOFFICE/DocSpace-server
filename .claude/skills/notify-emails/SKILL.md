---
name: notify-emails
description: "Adding or changing a DocSpace notification letter (email/telegram/push text): notify Action, subject_/pattern_ resource keys, tag substitutions, textile/HTML markup, periodic after-registration and tariff letters, and the Files module's own letters. USE FOR: add a new email, new letter, new notification, change email text, send letter N days after registration, add tags to a letter, wording of an editor-mention / document-shared / room-activity / form-filling notification. DO NOT USE FOR: SMTP/sender configuration, whats-new digest data collection, push delivery plumbing or socket notifications."
---

# Notification Letters

A letter = **notify Action** (id + patterns + tags) + **two resource keys** (`subject_*`, `pattern_*`)
+ a **call site** that resolves the action and sends it. Nothing else has to be registered: `[Scope]`
is enough for DI.

## Files

| What | Where |
| --- | --- |
| Actions | `web/ASC.Web.Core/Notify/Actions.cs` |
| Letter tests + their harness | `common/Tests/ASC.Notify.Tests`, shared parts in `Infrastructure/` (§9) |
| Texts (default culture) | `web/ASC.Web.Core/PublicResources/WebstudioNotifyPatternResource.resx` + hand-written `.Designer.cs` |
| Common tags for every letter | `web/ASC.Web.Core/Notify/NotifyConfiguration.cs` (`NotifyTransferRequest.BeforeTransferRequestAsync`) |
| Tag helpers (button, signature, image) | `web/ASC.Web.Core/Notify/TagValues.cs` |
| One-off letters (events) | `web/ASC.Web.Core/Notify/StudioNotifyService.cs` |
| Periodic letters (after registration / tariff) | `web/ASC.Web.Core/Notify/StudioPeriodicNotify.cs` |
| Cron registration | `web/ASC.Web.Core/Notify/StudioNotifyServiceSender.cs` |
| Master HTML template | `common/ASC.Core.Common/Notify/Stylers/Resources/NotifyTemplateResource.resx`, key `HtmlMaster` |
| Email styler + markup | `common/ASC.Core.Common/Notify/Stylers/TextileStyler.cs`, `common/ASC.Core.Common/Notify/Textile/` |

**Files has a second, parallel set of letters** — mentions, sharing, room activity, form filling — with
its own notify source, its own resx and its own conventions. If the letter you are after is about a
document or a room, go to §10 first; §2–§5 (resources, markup, substitutions, culture) apply to both.

## 1. Action

```csharp
[Scope]
public sealed class MyLetterNotifyAction(StudioNotifyHelper studioNotifyHelper, TenantManager tenantManager) : NotifyAction(tenantManager)
{
    public override string ID => "my_letter";          // snake_case, matches the resource key suffix

    public override List<Pattern> Patterns
    {
        get =>
        [
            new EmailPattern(() => WebstudioNotifyPatternResource.subject_my_letter, () => WebstudioNotifyPatternResource.pattern_my_letter),
            new TelegramPattern(() => WebstudioNotifyPatternResource.pattern_my_letter_tg)   // optional
        ];
    }

    public void Init(UserInfo user) { Tags = [ /* ... */ ]; }
}
```

Pattern → styler mapping is fixed in `common/ASC.Core.Common/Notify/Patterns/Pattern.cs`:
`EmailPattern` → `TextileStyler`, `TelegramPattern` → `MarkDownStyler`, `PushPattern` → `PushStyler`,
`JabberPattern` → `JabberStyler`. A telegram variant needs its own `pattern_*_tg` key — the same
body rarely survives both stylers.

**A `pattern_*_tg` twin is yours to keep in sync, by hand, in every culture.** `LetterTestBase` takes
an `EmailPattern` and nothing else, so none of the nine telegram keys is rendered by any test: a twin
can sit untranslated for years and every check stays green. Whenever you touch a letter, grep the resx
for its `_tg` sibling and carry the same edit across — including into the localized files, which is the
one case where hand-editing a translation is right, because the wording is already there in the
sibling key of that same culture.

For periodic letters inherit `BasePeriodicNotifyAction` (`Actions.cs`, bottom half) and answer the
three questions a scheduled letter has to answer about itself:

```csharp
[Scope]
public sealed class MyPeriodicLetterNotifyAction(
    UserManager userManager, StudioNotifyHelper studioNotifyHelper, ITariffService tariffService,
    CommonLinkUtility commonLinkUtility, PeriodicNotifyAction periodicNotifyAction, TenantManager tenantManager)
    : BasePeriodicNotifyAction(userManager, studioNotifyHelper, tariffService, periodicNotifyAction, tenantManager)
{
    public override string ID => "my_periodic_letter";
    public override List<Pattern> Patterns { get => [ /* EmailPattern */ ]; }

    protected override bool ToAdmins => true;                 // + ToOwner / ToUsers / ToGuests / ToPayer
    protected override bool RequiresSubscription => true;      // must be set: the base defaults to false
    protected override bool TrulyYoursAsTableRow => true;      // true for the HTML letters

    /// <summary>Is today this letter's day for this portal?</summary>
    public override Task<bool> ShouldSendAsync(PeriodicLetterContext context)
        => Task.FromResult(context.CreatedDate.AddDays(4) == context.NowDate);

    protected override Task AddTagsAsync(PeriodicLetterContext context, UserInfo user, CultureInfo culture, List<ITagValue> tags)
    {
        tags.Add(TagValues.OrangeButton(Resource("ButtonGetStarted", culture), commonLinkUtility.GetFullAbsolutePath("~/billing/overview")));

        return Task.CompletedTask;
    }
}
```

Then add the type to `_saasLetters` or `_enterpriseLetters` in `StudioPeriodicNotify` — that list *is*
the registration; nothing else schedules it.

Only the tags the letter's own pattern references. The base adds `$Culture`, `$UserName`, `$TrulyYours`
and `$Footer` for every periodic letter (`BuildCommonTagsAsync`), and `$Footer` is chosen from the
recipient — `common` for a DocSpace admin, `social` for anybody else — so it is not yours to set. An
unused tag is dead weight; a missing one leaves a raw `$URL1` in front of the reader.

**There are no shared `Init` overloads any more.** Each letter used to be a branch in one `else if`
chain per edition that filled forty shared locals and poured them into a thirty-eight parameter `Init`,
so every letter carried the union of every tag any letter might want. If you find a description of
`$URL1`…`$URL14` as "what a periodic letter gets for free", it predates that.

## 2. Resource keys

Add to the **default-culture `.resx` only** — localized files are filled by translators, never by hand:

- `subject_<id>` — subject line
- `pattern_<id>` — body
- optional `Button*` key for the button label, reused across letters when the wording matches

Rules:

- **Keys are sorted alphabetically** in the resx (a repo-wide convention, enforced by review).
  `Button*` keys sit in their own alphabetical run near the top.
- **`Resource.Designer.cs` is NOT regenerated by `dotnet build`** — add the property by hand, in the
  same alphabetical position, with the generator's doc-comment shape
  (`/// Looks up a localized string similar to <first line> [rest of string was truncated]&quot;;.`).
  Forgetting this gives "no such member" at the `Patterns` getter.
- The resx value is XML-escaped HTML: write `&lt;tr …&gt;`, and `&amp;amp;` when the rendered HTML
  needs `&amp;`. `&#8226;` / `&#8211;` decode straight to • / – .
- Mirror the text in `<comment>` (plain-text version) as the existing letters do — translators and
  the Designer doc-comment use it.

## 3. Markup

Two styles coexist; match the neighbouring letters:

- **Textile** (older / transactional letters) — see `common/ASC.Core.Common/Notify/Textile/`:
  `"text":"url"` → link, `*text*` → bold, `h1.Text` → heading, `#foreach($x in $List) … #end`,
  `#if(…) … #end`. **Bullet lists work**: `* item` at the start of a line (asterisk + space) becomes
  `<li>`, `# item` an ordered one — `States/UnorderedListFormatterState.cs`,
  `States/OrderedListFormatterState.cs`. The space is what tells a list from bold: `*Your login*:` at
  the start of a line stays inline bold precisely because no space follows the asterisk.

  **In ja-JP, ko-KR and zh-CN put a space on both sides of a `"text":"url"` link.** Those languages
  write no space around a link, and the parser needs a boundary: with a particle glued to the
  closing quote — `"決済方法":"$URL1"に`, `"결제 수단":"$URL1"의`, `"付款方式":"$URL1"中` — the link is
  not recognized and the letter prints the raw quotes and the URL instead. A slightly loose space
  before the particle is the lesser evil. The three are only the usual suspects — a Finnish or
  Azerbaijani case suffix and an Armenian hyphen break a link the same way. `AssertLinksRendered` (§9)
  catches all of it in every culture, so the preview run is the check, not eyeballing the source.

  **ar-SA opens such a caption with `{white-space: nowrap}` — keep it.** Textile reads a leading
  `{…}` as inline CSS for the phrase, and the Arabic file uses it in some thirty places, always where
  the caption *is* the portal address:
  `"{white-space: nowrap}${__VirtualRootPath}":"${__VirtualRootPath}"`. It stops the long URL from
  breaking across lines in right-to-left text. It looks like stray markup and is easy to drop while
  rewriting a sentence; **ar-SA is the only culture that uses this textile form**, so a diff that
  removes it from one Arabic key will not look wrong on its own. (Plain `white-space: nowrap` inside an
  HTML `style="…"` attribute is a different thing and is common everywhere — grep for the braces.)
- **Raw HTML table rows** (all marketing / after-registration letters) — a sequence of
  `<tr border="0" cellspacing="0" cellpadding="0"><td class="fol" style="…">…</td></tr>` blocks
  separated by blank lines, injected into `HtmlMaster`. There are no `<ul>`/`<li>` anywhere in the
  file: bullets are `•` + `<br />`, columns are nested tables. Copy the `style="…"` strings from a
  neighbouring letter instead of inventing new ones (Open Sans stack, 14px/21px body, 24px/700
  title, 40px side padding, `#FF6F3D` accent).

## 4. Substitutions

> ### ⚠ NEVER TRANSLATE A SUBSTITUTION
>
> **Tag names, the Velocity keywords around them and the literals they are compared against are
> code, not prose.** `$UserName`, `#foreach($activity in $Activities)`, `#if($AutoRenew == "True")`,
> `#end` stay byte-identical in every culture — translate only the words *around* them.
>
> A translated token fails silently: the reader gets a raw `$Datum` where a date belonged, or an
> empty activity digest because `#foreach($activity em $Activities)` stopped being a directive.
> Velocity is **case-sensitive**, so `#If(` is dead too, and it takes its orphaned `#end` with it.
> Real damage found in the localized files: `$NombreUsuario`, `$Meno vlastníka`, `$VonesëPagese`,
> `$aktivitāte`, `$Invite Link` and `$ Body` split by a stray space, `#final` instead of `#end`,
> `== "Verdadeiro"` instead of `== "True"`.
>
> The same goes for a lost `$`: `"{__VirtualRootPath}/billing/addons"` prints the path literally.
> The letter tests (§9) run over every culture by default, so they catch this — unless you narrowed
> the run with `LETTER_CULTURES`.

Per-letter tags are set in `Init` (`new TagValue("URL1", …)`), plus helpers from `TagValues`:
`OrangeButton(text, url, tag)`, `TrulyYours(helper, text, asTableRow)`, `Image(...)`,
`WithoutUnsubscribe()`.

Common tags come free from `NotifyConfiguration` — exact names in
`web/ASC.Web.Core/Notify/CommonTags.cs`, note that only some carry the `__` prefix:
`${__VirtualRootPath}`, `${__VirtualRootHost}`, `${__HelpLink}`, `${__SupportLink}`,
`${__SalesEmail}`, `${__SupportEmail}`, `${__SiteLink}`, `${__DateTime}`, `${__AuthorName}`,
`${LetterLogoText}`, `$Culture`, `$ProfileUrl`, `$ImagePath`, `$RecipientSubscriptionConfigURL`.
`${__AuthorName}` is whoever triggered the notification — the inviter in the room and agent letters —
and is filled for every letter, not only those.

**Never write the product name or a URL into the pattern text.** Two hard rules, both enforced by the
letter tests (§9):

- The word `ONLYOFFICE` must always be `${LetterLogoText}`, so a white-labelled portal sends its own
  branding. It works everywhere, including inside tag values, because
  `NotifyTransferRequest.BeforeTransferRequestAsync` resolves it in the tag values **before** the
  pattern is rendered — that is why `ButtonGoToDocSpace` can be `Go to your ${LetterLogoText}` and
  `TrulyYoursText` can be `Truly Yours, ${LetterLogoText} Team`. It has to happen there rather than on
  the finished body: `MarkDownStyler` escapes the braces for Telegram's markdown, so after styling
  nothing can recognise the reference any more — which is how the wallet letter came to be signed
  "Truly Yours, ${LetterLogoText} Team" on Telegram while the mail was correct.
- External links come from `externalresources.json` (`ExternalResourceSettingsHelper`, resolved for the
  recipient's culture) and portal links from `CommonLinkUtility` — passed in as `$URL1`, `$URL2`, … or
  read off the common set (`${__VirtualRootPath}`, `${__SupportLink}`). A hard-coded `https://…` in a
  pattern breaks regional domains and white-labelling. There is no fixed pool of `$URLn` slots: a letter
  numbers only the links it has, and today nothing goes past `$URL2`.

**`${LetterLogoText} Docs` is a product name, not a phrase.** The editors carry that name in every
culture, so `Docs` stays untranslated — including where the reference writes it bare, as in "Embed
Docs into your ecosystem". The one exception is `zh-CN`, which may render the name as
`${LetterLogoText} 文档`. Do not read the tariff table of `saas_admin_activation_v1` as a precedent:
there `Docs` heads a list of app names (Files, Rooms, Forms, AI agents) and is translated along with
them.

`$Footer` selects the footer block in `TextileStyler`: `"common"`, `"social"`, `"opensource"`, or
`null` for none.

`$TopGif` is the image above the letter (`studioNotifyHelper.GetNotificationImageUrl("x.gif")`).
Image assets are **not in this repo** — they are served from `web:notification:image:path`. Leaving
it empty is fine: the tenant letter logo is rendered instead.

## 5. Culture

The letter is rendered in the **recipient's** culture. Anything pulled from another resource file
must be resolved with that culture and passed in as a tag:

```csharp
var culture = GetCulture(user);                                                    // one-off letters
WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", culture);
```

```csharp
Func<CultureInfo, string> orangeButtonText = c => WebstudioNotifyPatternResource.ResourceManager.GetString("ButtonAccept", c);
Func<CultureInfo, string> url1 = c => externalResourceSettingsHelper.Site.GetRegionalFullEntry("openai", c);
```

Periodic senders pass `Func<CultureInfo, string>` precisely because the culture is only known per
recipient inside the send loop — never capture `CultureInfo.CurrentCulture` at setup time.

## 6. Sending

**Event-driven letter** — add a method to `StudioNotifyService`:

```csharp
var action = serviceProvider.GetService<MyLetterNotifyAction>();
action.Init(user);
await studioNotifyServiceHelper.SendNoticeToAsync(action, [recipient], [EMailSenderName]);
```

**Periodic letter** — `StudioPeriodicNotify`, one method per edition, all scheduled from
`StudioNotifyServiceSender.RegisterSendMethod` with `core:notify:cron` (default 5am daily) and
disabled entirely by `core:notify:tariff=false`:

- `SendSaasLettersAsync` (SaaS) and `SendEnterpriseLettersAsync` (Enterprise/Developer, trial letters
  additionally require `defaultRebranding`) — the only two. `RegisterSendMethod` picks **one** of them
  per installation, `tenantExtraConfig.Enterprise` first, then `.Saas`; there is no opensource sender
  (the old `SendOpensourceLettersAsync` went away with its last letter).

Each method gathers a `PeriodicLetterContext` per tenant — tariff, quota, dates, last activity — and
then asks **every** letter in its list whether today is its day (`SendLettersAsync` →
`ShouldSendAsync` → `SendAsync`). So adding a letter is two edits and neither is in this file's control
flow: implement it as in §1, and add its type to `_saasLetters` / `_enterpriseLetters`.

**Letters now judge themselves independently, and that changes what a predicate has to say.** There is
no enclosing `if` to inherit a condition from any more, so a predicate must carry every condition its
old branch got for free from the chain it was nested in — the tariff state, the free quota, the trial.
A predicate that only checks the date will fire on portals the letter was never meant for.

The flip side is that the mutual exclusion the chain gave away is gone too: two letters can now claim
the same portal on the same day. `PeriodicLetterScheduleTests.OnlyOneEnterpriseLetterClaimsAPortal` is
where that is checked, and a new letter belongs in it.

**The base default is `RequiresSubscription => false`**, i.e. the letter goes out whatever the recipient
has switched off — right for billing and legal notices, wrong for everything else. A marketing letter
has to override it to `true`, and forgetting to is not a compile error: the letter simply ignores an
unsubscribe.

## 7. Recipients (`StudioNotifyHelper.GetRecipientsAsync(toadmins, tousers, toguests)`)

| flags | result |
| --- | --- |
| `(true, false, false)` | `GroupAdmin` members = DocSpace admins **incl. the portal owner** |
| `(true, true, false)` | `EmployeeType.RoomAdmin` only — **DocSpace admins are excluded** (easy to get wrong) |
| `(true, false, true)` | `GroupAdmin` members ∪ `EmployeeType.Guest` |
| `(false, true, false)` | room admins minus `GroupAdmin` |
| `(true, true, true)` | everybody |

The owner is put into `GroupAdmin` at tenant creation (`DbTenantService`), so `ToAdmins` normally
covers it; `ToOwner` unions the owner in explicitly (`users.Append(owner).DistinctBy(u => u.Id)`) and
`ToPayer` adds the Stripe customer. For a periodic letter these are the properties from §1 —
`BasePeriodicNotifyAction.GetRecipientsAsync` reads them and calls the helper above.

## 8. Verify

```bash
dotnet build web/ASC.Web.Core/ASC.Web.Core.csproj -p:EnforceCodeStyleInBuild=true --no-dependencies
```

```bash
dotnet format style web/ASC.Web.Core/ASC.Web.Core.csproj --include web/ASC.Web.Core/Notify/Actions.cs --verify-no-changes
```

Check the resx parses and the markup decodes as intended — an unescaped `&` or `<` makes the whole
file invalid XML and fails the build on resource generation:

```powershell
$x = [xml](Get-Content web/ASC.Web.Core/PublicResources/WebstudioNotifyPatternResource.resx -Raw)
($x.root.data | Where-Object { $_.name -eq 'pattern_my_letter' }).value
```

Everything in the working tree is **CRLF** — resx and `.cs` alike (`core.autocrlf=true` stores LF in
the repository and converts on checkout). Write CRLF, and never let an editor re-save a resx with LF:
the diff then covers the whole file and buries the one line that actually changed.
`git diff --check` reports "trailing whitespace" on every added line in this repo (same `autocrlf`
artifact); confirm real trailing whitespace with a grep before chasing it.

Runtime check: MailPit catches outgoing mail in local dev (the Aspire dashboard shows the port it
was published on); `../Logs/notify.log` shows what the notify service did. The letter tests (§9) do not
piggyback on that stack — they start one of their own.

## 9. Look at the letter (preview test)

**Every new letter gets a test in `common/Tests/ASC.Notify.Tests`.** The harness resolves the real
action, calls its own `Init`, renders the letter through the real pipeline (resources →
`NVelocityPatternFormatter` → `TextileStyler` → `HtmlMaster`), saves the HTML, delivers it to MailPit and
asks MailPit how many mail clients can render it. It is a rule for new letters, not a statement about the
old ones: **51 of the 122 actions that carry an email pattern have a test**, so a green run means the
covered letters are fine, not all of them.

```bash
dotnet test common/Tests/ASC.Notify.Tests/ASC.Notify.Tests.csproj
```

**Docker has to be running.** The suite boots the Aspire AppHost itself on the `integration-test` launch
profile, like the Files/People/AI suites, and trims the graph to what a letter needs: MySQL, the
migration runner, ApiSystem, MailPit — plus RabbitMQ, Redis and OpenSearch, which stay only because
every project in the graph waits for them. Then it registers a portal through `portal/register` and
builds the DocSpace service graph **inside the test process** (`Infrastructure/LetterHost.cs`).

That is not gold-plating: a notify action is a `[Scope]` service whose `Init` resolves links against the
current tenant and shortens them through the database, so there is no calling it without a portal. Start
to first test is about 50 s; the full sweep of ~3400 cases takes some two and a half minutes.

When the whole suite goes red, `LetterStackSmokeTests` is the test that says whether the stack broke or
the letters did — it registers a portal, resolves one action and calls `Init`, and nothing else.

**That run covers every culture the portal offers** — `LetterCultures.Names` reads `web:cultures` from
`appsettings.json` (falling back to the cultures whose `ASC.Web.Core.resources.dll` satellite sits next
to the binaries, and throwing if neither answers). A new language is therefore covered the day it is
switched on. Checking English alone would be close to useless: the defects these tests exist to catch —
a translated tag name, a lost `$`, a textile link glued to the next character — cannot occur in the
default culture at all.

`LETTER_CULTURES` **narrows** the sweep for one run while you iterate:

```bash
LETTER_CULTURES=en-US,de,ru,zh-CN dotnet test common/Tests/ASC.Notify.Tests/ASC.Notify.Tests.csproj
```

Only `AssertContent` (tags resolved, links and buttons present) runs per culture;
`AssertDefaultCultureText` checks wording and stays on `en-US`, since any other culture may legitimately
translate it. A culture with no resx of its own falls back to the default one, exactly as production does.

**A letter test says how `Init` is called and what the text must contain — never what the tags are.**
That is the whole shape of it; everything shared lives in `Infrastructure/`:

```csharp
public class SaasUserWelcomeLetterTests : LetterTestBase<SaasUserWelcomeV1NotifyAction>
{
    protected override Task InitAsync(SaasUserWelcomeV1NotifyAction action, LetterScope scope)
    {
        action.Init(scope.Recipient);          // the production call, with the production arguments

        return Task.CompletedTask;             // async Init: return action.Init(...) instead
    }

    protected override void AssertContent(RenderedLetter letter, LetterScope scope) { /* links, any culture */ }

    protected override void AssertDefaultCultureText(RenderedLetter letter, LetterScope scope) { /* English wording */ }
}
```

There is no `Init` on `INotifyAction` — every action declares its own signature — so that one call is the
only thing a test cannot inherit. Everything the letter says follows from it: the button, the footer
flavour, the top image, whether the signature is a table row.

A periodic letter has no `Init`; derive from `PeriodicLetterTestBase<TAction>` and it calls
`BuildTagsAsync` for you. Override `BuildContext` only when the letter quotes a date — the portal state
is assembled in memory by `PeriodicLetterContexts`, shared with the schedule tests, so a letter about an
expiring tariff needs no portal whose tariff actually expires.

`LetterScope` is that test's view of the portal: `Recipient` (the owner, carrying the culture under
test), `DisplayName`, `PortalUrl`, `Culture`, `Services`. Letters disagree about which name they greet
with — the welcome ones use `Recipient.FirstName`, the backup ones `DisplayName` — and that difference is
only visible because the harness no longer fills `$UserName` itself.

**Do not restate a tag value in an assertion when you can assert the effect.** `AssertContent` is
optional, and for the six letters whose `Init` shortens its link there is nothing stable to assert: the
short key is minted by the database on every call. Assert the button caption and the portal address
instead.

Shaping the input is fair game, and sometimes necessary: `SaasAdminActivationV1NotifyAction.Init` offers
the password change only to an activated owner with an audit date and otherwise asks for email
confirmation, so its test clones the recipient and sets `ActivationStatus` to pick the variant it means
to render.

**The template is never named in the test.** `LetterTestBase<TAction>` reads it off the action itself —
`Patterns.Find(SenderName == "email.sender")`, the same lookup `NotifyEngine` does — and takes the
preview name from `TAction.ID`. Since the pattern XML was removed the action is the only place a
letter's `subject_`/`pattern_` keys are written down, so repeating them in the test would let it keep
rendering an old template after the action moved to a new one, and stay green.

A letter with no `EmailPattern` therefore cannot be rendered by this harness at all — `Pattern` throws.
That is most of the Files module (§10), which is push-first.

Where one template serves several actions — the activation and welcome letters, one per edition — the
test names the edition it renders, and its `<summary>` says which others share the template.

The **common** tags are not restated either: the harness builds a real `NotifyRequest` and runs
`NotifyTransferRequest.BeforeTransferRequestAsync` over it, the same step the engine runs on the way out.
So the portal paths, the author, the external links, the branding and the letter logo are the production
values, and `Init`'s tags go in first — which is what lets the transfer step see a letter's `$TopGif` and
leave its logo alone.

That step is also why a letter **without** a top image shows `cid:…` rather than a file under the image
folder: `AddLetterLogoAsync` attaches the tenant logo and references it by content id. The MailPit
delivery carries that attachment, so the message under test is the one a reader would receive.

Two tests are generated per culture:

- **`Letter_Renders`** — always runs. Checks, for free: no raw `$Tag`/`${Tag}` survived (tag list taken
  from the pattern itself), the product name is not hard-coded, no `http(s)://` is hard-coded in the
  pattern, **every textile link actually became an `<a href>`** (`AssertLinksRendered` — an unrecognised
  `"caption":"url"` still renders and still contains the address, so nothing else catches it), the top
  image / letter logo, the signature. Then your `AssertContent`, and
  `AssertDefaultCultureText` for `en-US` only (another culture may carry a translation). Saves
  `bin/Debug/net10.0/letter-preview/<letter-id>.<culture>.html`.
- **`Letter_IsDeliveredToMailPit`** — sends the letter over SMTP, asserts it arrived, prints the message
  URL, then calls MailPit's `html-check` and asserts the share of mail clients that render the markup
  (`MinimumHtmlSupport`, 75% by default). The letters measure 78.5–91.5%; nearly all of the remainder is
  *partial* support, the CSS every table-based email leans on, and under 3% is unsupported outright. The
  floor sits just below the lowest letter — low enough that a caniemail data update alone cannot turn the
  suite red, high enough that markup costing a letter more than a few points has to be justified. A
  letter may raise or lower it with `protected override double MinimumHtmlSupport`, and the override says
  why. When it fails, the message names the offending constructs (`css-margin`, `css-display`, …), so
  read those before touching the threshold.

`Infrastructure/LetterEnvironment.cs` holds the **expected** side of the assertions, read from the config
the local stack runs on: branding from `BaseWhiteLabelSettings.DefaultLogoText`, external links from
`buildtools/config/externalresources.json` (with fallbacks when it is absent). It is deliberately *not*
the source the letters are rendered from any more — that is the portal — so a help-center link the two
disagree about is a finding, not a duplication.

**A letter test must not hard-code a URL.** Portal addresses come from `scope.PortalUrl`; external ones
from `NotificationImageUrl`, `ExternalDomain`, `ExternalEntry`. (`PortalLink` is gone — it was a second
implementation of `CommonLinkUtility.GetFullAbsolutePath`.) `PortalUrl` survives as an input, the address
that seeds the request the sending code would have had; a registered portal still answers there because
`core:base-domain` is `localhost`, which `Tenant.GetTenantDomain` short-circuits on whatever the alias is.
`LetterScope` asserts that on every scope it opens, so if that ever stops being true the failure names it.

`AssertDefaultCultureText` compares against the **rendered** body, not the resx source: `TextileStyler`
applies typographic replacements, so a straight `'` in the pattern arrives as `&#8217;` and an assertion
on `haven't` fails. Pick expected substrings without apostrophes or quotes.

`Infrastructure/LetterCultures.cs` derives the culture list from `web:cultures`, so a new language needs
no edit there — only `LETTER_CULTURES` narrows a single run.

**What the preview does not cover.** The recipient is always the portal owner, who is a DocSpace admin,
so the `social` branch of a periodic letter's footer is never taken — it matters to
`saas_admin_user_apps_tips_v1` and its Enterprise twin, which also go to plain users. The portal is never
white-labelled, so `AddLetterLogoAsync`'s other branch (custom logo, `$TopGif` dropped) never runs. Only
the `EmailPattern` is rendered — telegram and push bodies are not, and what little covers them is
`NotifyStylerTests` in `common/Tests/ASC.Core.Common.Tests`: a pattern through the formatter and
`MarkDownStyler`/`JabberStyler`, no portal, asserting what the email tag values turn into. And the
schedule is a separate suite: `PeriodicLetterScheduleTests` answers *when* a letter goes out, this
one *what it says*.

## 10. The Files module's own letters

Everything above describes the studio source in `web/ASC.Web.Core`. Files runs a **second notify source**
for anything about a document or a room — mentions, sharing, room removal, form filling, the push feed:

| What | Where |
| --- | --- |
| Actions **and** the tag-name constants | `products/ASC.Files/Core/Services/NotifyService/NotifyConstants.cs` (one file — `NotifyConstants` itself sits at the bottom) |
| Texts | `products/ASC.Files/Core/Services/NotifyService/FilesPatternResource.resx` + hand-written `.Designer.cs` |
| Source (its own GUID) | `.../NotifyService/NotifySource.cs` |
| Call sites | `.../NotifyService/NotifyClient.cs` |
| Per-room debouncing | `.../NotifyService/NotifyEventQueue.cs`, `RoomNotifyEventQueue.cs` — what collapses a burst of uploads into one letter with a `$Count` |

Everything in §2–§5 applies unchanged: same styler and `HtmlMaster`, same textile, same alphabetically
sorted resx with a hand-edited `Designer.cs`, same `${LetterLogoText}`, same never-translate-a-substitution
rule. What you have to do differently:

- **Name the id in PascalCase**, not snake_case — `EditorMentions`, not `editor_mentions`. The resource
  keys follow it: `subject_<Id>`, `pattern_<Id>`, and a push body under `<Id>_push`.
- **Check the neighbouring action for the push key's prefix.** Both `pattern_X_push` and `subject_X_push`
  are in use; there is no rule to derive it from, so copy rather than guess.
- **Declare only the channels your letter needs.** One pattern is as normal as three — a quiet room event
  is push only, a report is mail only. Nothing has to be filled in for symmetry.
- **Then name the senders at the call site**, or the pattern is inert: `SendNoticeToAsync` takes the
  sender list explicitly and push goes out through its own `SendNoticeAsync(..., NotifyPushSenderSysName)`.
  Adding a `PushPattern` and stopping there sends nothing.
- **Point a telegram pattern at the email body** — `new TelegramPattern(() => …pattern_<Id>)` — the way
  the existing ones do. That spares you the `_tg` twin of §1, at the cost of a body that has to survive
  `MarkDownStyler` as well. (Some orphaned `subject_*_tg` keys are still lying around from an older
  arrangement; they are referenced by nothing, so don't take them as a model.)
- **Take tag names from `NotifyConstants`, never spell them yourself.** The casing is not guessable —
  `$DocumentURL` and `$RoomURL` shout the URL, `$FolderID` and `$FolderParentId` disagree with each other.
- **Don't re-register anything for common tags.** `NotifyConfiguration` hooks
  `WorkContext.NotifyClientRegistration` globally and registers `NotifyTransferRequest` on the engine, so
  `${__VirtualRootPath}`, `${__AuthorName}`, the URL absolutizer and the `${LetterLogoText}` resolution
  reach this source too.
- **Read the pattern before assuming which tag names a person.** An action can hand the same human to its
  mail body and its push body under different tags — one from the global set, one set by `Init`.

Sending, from `NotifyClient`:

```csharp
var client = notifyContext.RegisterClient(serviceProvider, notifySource);
var recipient = await notifySource.GetRecipientsProvider().GetRecipientAsync(userId.ToString());

var action = serviceProvider.GetService<EditorMentionsNotifyAction>();
action.Init(file, plainText, currentUser, documentUrl, folderTitle, folderUrl);

await client.SendNoticeAsync(action, file.UniqID, recipient);   // + bool checkSubscription overload
```

A send method is not obliged to map to one letter, and before writing a new one, check whether an
existing method already covers your case. One event may owe notices to several parties — each its own
action, in its own culture, on its own channels — and a family of near-identical events is served by a
single method that takes the action type as a parameter, so a new member of that family needs a subclass
and a resource pair rather than any new sending code.

**The gates are hand-written in `NotifyClient`, not in the action** — a new room letter that omits them
reaches people who muted the room:

- `fileSecurity.CanReadAsync(file, recipientId)` — never notify about something the reader cannot open;
- `studioNotifyHelper.IsSubscribedToNotifyAsync(recipientId, RoomsActivityNotifyAction)` — note the
  subscription is checked against **`RoomsActivityNotifyAction`**, a studio action, not against the letter
  being sent;
- `roomsNotificationSettingsHelper.CheckMuteForRoomAsync(roomId, recipientId)` for `VirtualRooms`.

**None of these letters has a test**, so the §9 checks — unresolved tags, hard-coded URLs, unrendered
textile links, every culture — cover the studio letters only. Editing one means previewing it by hand
through MailPit (§8).

Closing that is more than pointing the harness at another resource class, and it is worth knowing why
before you try:

- the actions **do** resolve in the §9 host — `ASC.Studio.Notify` references `ASC.Files.Core`, so
  `DIHelper.Scan` registers them, `FilesLinkUtility` and `FileUtility` included;
- but **16 of the 24 have no `EmailPattern`** (push only, and `ShareEncryptedDocument` has an empty
  `Patterns`), and `LetterTestBase` throws on those. Covering them needs a push-rendering harness, not
  this one;
- and the eight that do have a mail body take `FileEntry<T>` / `Folder<T>` / `File<T>` as `Init`
  arguments. Those `Init` bodies only read `Title`, `Id`, `ParentId`, `RootFolderType`, so hand-built
  entries may well be enough — but `DocuSignComplete` and the form-filling family route through
  `FilesLinkUtility.GetFileWebPreviewUrl` and `FileUtility`, which holds a `DaoFactory`, and whether that
  path touches the database is unverified.

`Resource` on the base reads `WebstudioNotifyPatternResource`, so a Files test would need its own
accessor for `FilesPatternResource` — that part really is a one-liner.

## Reference letters to copy from

- **HTML marketing letter, tariff-independent, day N after registration**:
  `saas_admin_handy_apps_v1` (no top image, one button) and `saas_admin_configure_v1` (top gif, extra
  `$URL1`/`$URL2` links) — the two blocks at the top of `SendSaasLettersAsync`, each with a test in
  `common/Tests/ASC.Notify.Tests`.
- **Long HTML letter, several sections and bullet lists**: `saas_admin_ai_agents_v1`,
  `saas_admin_handy_apps_v1`.
- **HTML letter with images (`$IMG1`…) and per-image links**: `saas_admin_user_apps_tips_v1`,
  `enterprise_admin_user_apps_tips_v1`.
- **Plain textile transactional letter**: `low_wallet_balance`, `password_changed`.
- **Files letter** (§10): `EditorMentions` for all three channels off one action, `FolderCreatedInRoom`
  for the push-only minimum.
- **Same letter for several editions**: nothing is shared right now — SaaS and Enterprise keep separate
  clones (`saas_admin_user_apps_tips_v1` / `enterprise_admin_user_apps_tips_v1`). When the text really is
  identical, prefer resolving one action from both senders over cloning it.
