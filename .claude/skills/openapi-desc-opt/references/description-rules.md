# Operation descriptions: the fewest words, no lost fact

These rules cover the operation `description` (`<remarks>` in C#) and the `When` text of
`[RaisesWebhook]`. §2 of `.claude/rules/openapi-endpoint-docs.md` is their short form; where the two
differ in detail, these rules win. Everything else of that rule still holds: where the text
comes from (§0), the title (§1), the mechanics of `[RaisesWebhook]` (§2.1), the response texts (§3.6),
consistency (§4).

Goal: the fewest words with no lost fact and nothing lost for an AI consumer, in English that a reader
with a basic level can follow.

## 1. Check before writing

1. **Start from the code of the branch.** Read the action and the services it calls. Do not trust the
   current description or the published JSON: either can be wrong or out of date. Then:
   - list the facts of the current description and verify each one you keep;
   - go through §2 item by item, check each against the code and the declared responses, and add what
     is missing, even when the current text never had it.
2. **Know what the agent sees.** OpenAPI→MCP converters pass the operation `description` (or
   `summary`), the parameters and body properties with their descriptions. Some also pass the schema
   of the 200 response. No converter passes response descriptions or headers.
3. **Find where each fact lives.** Look at the parameters, the body properties, the nested schemas and
   array items of the body, and the properties of the 200 response.
   - **A fact about the value itself** may stay only in the parameter or property: what it means,
     where a valid one comes from, its format, its default, its length or range.
   - **A fact about behaviour stays in the description**, even when a parameter states it too: what the
     call does with the value, how it compares it, which parameter wins, what an empty or zero value
     means, what switching a flag does, what a repeat does.
   - **A fact only in a response description or header**, or nowhere else, stays in the description.
4. **Read the text without the parameters.** Every sentence must make sense without them. Do not leave
   a reference to a fact that only a parameter states ("turning it back on restores access", "minimums
   apply"). State the fact, or name the parameter: "the minimums of `quantity`".
5. **Compare the status codes.** Every 4xx and 5xx code of the current text and of the declared
   responses must be in the new text, or have a reason to be left out. Usual reasons: 401 for a missing
   sign-in, 500, 502 and 503 of the pipeline, a code the type of a parameter already rules out.
   - **The text of a `[SwaggerResponse]` and the old description are not sources of a fact.** They
     only say where to look. Check each code and each condition you write in the code of the action
     and the services it calls.
   - A declared code is a candidate, not proof. If a path of the action returns it, name it in the
     text. If no path returns it, remove the attribute.
   - A code that the text gives to another operation (`` `GET api/2.0/x` returns 404 ``) is not a
     code of this one.

   `check_texts.py` lists the codes of both texts, of the responses and of the parameters (`CODES`
   lines). Each `CODES` line is a question to the code of the action, not a phrase to add.

## 2. What stays, in this order

The order is the one of `.claude/rules/openapi-endpoint-docs.md` §2. Leave an item out only when it
does not apply.

1. **What the operation does**, in the first sentence, with a present-tense verb (GET → Returns,
   POST → Creates/Connects/Starts, PUT → Changes/Updates/Renames, DELETE → Deletes) and the resource
   and scope.
   - Do not claim an effect that the operation does not always have. If the effect depends on the
     caller or a mode, say so in the first sentence: "Changes the email address of an account, or
     sends a link that confirms the change."
   - Modes that define what the call does stay here, even when a parameter describes them too
     (`Add` / `Set`).
   - A second sentence may explain the first one ("This is a wallet setting, not the state of the
     backup service.").
   - A deprecated operation says so in the first sentence and names the replacement.
   - If an older description is correct, start from it.
2. **Preconditions**, with the operation that satisfies them.
3. **Access** — the phrase from §5.1, then the refusal code in its own sentence: `Other callers get
   403.` Check in the code which code a refusal really returns.
4. **Effect** — read-only, changes data, spends money, cannot be undone, or queues work. Then:
   - **for a call that changes data, whether it can be repeated**: `You can repeat it with the same
     body.` / `Do not repeat it. Each call sends the emails again.` / `A repeated call returns 404.`;
   - **for a status call, what to poll and until when**: `This is the call to poll. Repeat it until
     isCompleted is true.`;
   - not `idempotent` next to `read-only`: a read can always be repeated.
5. **What comes back**, only what the schema does not make clear:
   - a body that is not JSON;
   - what `true` and `false` mean;
   - the order of a list, paging and whether there is a total;
   - what an empty result means;
   - fields that are filled only in some states;
   - a list that includes skipped items;
   - the meaning of a response field when the schema text does not give it.

   When the body DTO is shared with other operations and this one reads only some fields, name them:
   `The call reads only userId and email from the body.`
6. **Limits and related operations**:
   - limits published only in responses, with the code (`By default, the limit is 10 calls a minute
     per user. More calls return 429.`);
   - what the operation does not do, and which operation to use instead: `To <task>, use METHOD
     api/2.0/….`
7. **Errors the caller can act on**, with the code as a number, in the form of §5.2.

## 3. What goes

- Facts about the value that a parameter or property already states (§1.3).
- Reasons (`because …`), restatements, the schema retold field by field.
- Internal side effects that the caller cannot see: socket pushes, audit records, internal caches. A
  cache that makes the answer stale is not internal: it stays.
- The webhook sentence: the generator writes it from `[RaisesWebhook]`.

## 4. Form

- **One paragraph**, no labels, no lists. The Markdown generator replaces line breaks with spaces.
- **Sentence length.** At most 20 words; aim for 8–12. Code in backticks counts as one word. Check the
  length with a tool, not by eye. Vale (`Docs.SentenceLength`) counts a hyphenated word (`top-up`) as
  two words; `check_texts.py` and the vacuum rule count the same way and add code as one word.
- **One link between facts per sentence, and the link is a word.** A colon is allowed only before a
  list. Any other link — an instruction and how to do it, a fact and its result, a condition and its
  error — becomes two sentences. Name the link when it is not obvious: `First`, `Then`, `So`, `To do
  this`, `After this call`.
- **Voice.** An action has its doer: `The call skips LDAP accounts`, not `LDAP accounts are skipped`.
  A state may be passive: `is turned off`.
- **Lists.** Three items or more take a comma before `and` / `or`: `API keys, OAuth apps, and
  webhooks`. A list never stands before the verb as the subject. Put the verb first and the list after
  it.
- **Conditions.** A short condition goes before the action: `If no tag has the old name, the call
  returns 404.` A list of conditions goes after the verb: `The call returns 400 if a name is empty, has
  only spaces, or is longer than 255 characters.`
- **Word order to avoid:**
  - `or` in the sense of "otherwise" (`…, or the call returns 403`) → `If you are not …, the call
    returns 403.`;
  - `unless` with a negation (`You cannot delete X unless you are Y`) → `Only Y can delete X.`;
  - inversion (`So does a blank name`) → `A blank name also returns 400.`;
  - a sentence that starts with code or a path → `To start a job, use POST api/2.0/….`
- **Plain words.** One direct meaning per word. Replace the expressions of §5.3.
- **Terms** from §5.4, one term per concept in all texts.
- **Notation.** Operations as `METHOD api/2.0/…`, status codes as numbers, names of fields and values
  in backticks.
- **Length.** At least 3 sentences. There is no word limit: the facts set the length, so the minimum
  grows with what the operation does. Bring the text to its minimum:
  - every sentence states a fact that no other sentence states;
  - the average is at most 12 words per sentence;
  - one more pass with the techniques of §5.5 removes less than 5% of the words without losing a
    fact.

  A text over 250 words is a signal, not a limit: check whether the details of a mode can move to the
  description of the enum value. The description keeps one short sentence per mode (§2.1).
- **Webhooks.** The `When` text of `[RaisesWebhook]` follows the same form: the whole sentence "Raises
  the `x` webhook <When>." has at most 20 words and an active verb. Two conditions mean two
  attributes, not one long `When`.

## 5. Phrases and terms

### 5.1 Access

| Kind of access | Phrase |
|---|---|
| user type | `Requires a DocSpace admin.` |
| permission | `Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.` |
| all users except guests | `Any signed-in user can call it, except a guest.` |
| every signed-in user | `Any signed-in user can call it.` |
| no sign-in | `Anyone can call it without signing in.` |
| a token from a link | `Sign the request with the confirmation token from the emailed link.` |
| access to an object | `Requires editing access to the file.` / `Requires the right to create content in the room.` |
| a role that seems enough but is not | `A room admin can create tags but also gets 403 here.` |

After the phrase: `Other callers get 403.`, or the code the refusal really returns. For `except a
guest`: `A guest gets 403.`

### 5.2 Errors

- `If <short condition>, the call returns N.`
- `The call returns N if <condition>, <condition>, or <condition>.`
- `It also returns N if <condition>.`
- `A repeated call returns N.`

One verb for all codes: `returns`. A caller may `get` a code: `Other callers get 403.`

### 5.3 Plain words

| Instead of | Write |
|---|---|
| `turns true`, `turns false` | `becomes true`, `becomes false` |
| `at once` | `immediately` |
| `goes to`, `go to` (a file, a letter) | `the portal saves … in`, `the portal sends … to` |
| `drops` (a record, a name) | `deletes`, or `no longer shows` — say which |
| `hand over` | `move … to another user` |
| `used up` | `after the portal spends …` |
| `by itself` | name the doer: `the portal turns it on` |
| `comes back again` | say what happens: `the call changes nothing and returns the profile again` |
| `in place` | `has` (`the installation has a license`) |
| `after a while`, `a normal session` | the number or the exact thing; if unknown, an instruction (`Request them again before each use.`) |
| `afterwards` | `After this call` |
| `also the skipped ones` | `including the skipped ones` |
| `erasure` | `deletion` |
| `ready-to-show`, `free-form` | `a text that you can show to the user`, `any` |
| `health` (of a service) | `if the service works` |
| `client` (a program) | `app` |
| a chain of nouns (`your own personal folder deletion`) | a verb (`the job that deletes your personal folder`) |

### 5.4 Terms

| Concept | Term | Source |
|---|---|---|
| user types | `DocSpace admin`, `room admin`, `user`, `guest` | `[Description]` of `EmployeeType` (`common/ASC.Core.Common/Core/EmployeeType.cs`) |
| roles in a room | `room manager`, `content creator`, `editor`, … | `[Description]` of `FileShare` (`products/ASC.Files/Core/Core/Security/FileShare.cs`) |
| the owner | `the portal owner` | no enum |
| one DocSpace | `portal` | — |
| the server with all its portals | `installation` | — |
| a person's record | `account`; the returned object is the `profile` | — |
| other "accounts" | `storage account`, `wallet sub-account` — never bare `account` | — |

When a text names both a user type and a room role, and they could be confused, write `the user type
room admin` or `the room role room manager`. A user type `room admin` is not a room role `room
manager`.

### 5.5 Shortening techniques

They make a text shorter without removing a fact, a status code, the refusal code or a repeat
statement, and without breaking §4.

| Technique | Instead of | Write |
|---|---|---|
| Several cases of one code in one sentence, after the verb | `An unknown top-up returns 404. A top-up without an invoice also returns 404.` | `The call returns 404 for an unknown top-up, a top-up without an invoice, or no billing customer.` |
| A precondition written as its error | `The portal needs a billing customer.` … `Without a billing customer, the call returns 404.` | only the second sentence |
| Each fact once | `…to the imported users who are not active yet. Active users never get one.` | the first sentence alone |
| No restatement (§3) | `…turns the service on. So the setting can change while nobody edits it.` | the first sentence alone |
| A limit inside its error sentence | `A name has up to 255 characters, and a value up to 8000.` … `…returns 400 for a too long name or value.` | `It also returns 400 for a blank or repeated name, a name over 255 characters, or a value over 8000.` |
| Two sentences with one subject merged, if together they have at most 20 words | `It also skips LDAP, system, and deleted accounts. So you can repeat the call.` | `It also skips LDAP, system, and deleted accounts, so you can repeat the call.` |
| A shorter wording with the same meaning | `If the account no longer exists, the call returns 404.` | `A deleted account returns 404.` |

A limit that the schema already publishes (`maxLength`) is a fact about the value (§1.3): the error
sentence may say `too long` without the number.

## 6. Check after writing

1. `check_texts.py` (SKILL.md step 4): its own checks, then textstat, Vale and vacuum where they are
   installed. A sentence over 20 words and a lost code are errors; the rest are places to look at.
2. Read the text once without the parameter descriptions (§1.4) and once for the order of §2.
3. Make one more pass with §5.5. If it removes 5% of the words or more, the text was not at its
   minimum: keep the shorter text, then run step 1 on it again.

## 7. Worked example: `PUT api/2.0/files/tags` (`updateRoomTag`)

A text that looks finished, 92 words:

> Renames a custom room tag in the portal, and every room with this tag shows the new name. Requires
> a DocSpace administrator. A room manager who can create tags still cannot rename them. The call
> changes data and returns the new name. If no tag has the old name, the call returns 404. Both names
> must be non-empty and up to 255 characters, or the call returns 400. Saved room filters keep the old
> name: a `tags` filter of `GET api/2.0/files/rooms` with it finds no rooms. To read the tag names, use
> `GET api/2.0/files/tags`.

What it lost or got wrong:
- the refusal code 403 (§2.3);
- that a repeat returns 404 (§2.4);
- two behaviour facts left in the properties: how the old name is matched, and that a taken new name
  returns 400 (§1.3);
- "room manager" is a room role, but the code checks the user type room admin (§5.4);
- `…, or the call returns 400` (§4), and the colon in "Saved room filters keep the old name: …" (§4).

The rewrite — facts checked in `CustomTagsService.UpdateTagAsync`, `CreateTagAsync`,
`TagDao.GetTagsInfoAsync` and `UpdateTagRequestDto`, then shortened by §5.5. 124 words, 13 sentences,
9.5 words per sentence, the longest 16:

> Renames a custom room tag of the portal. Every room with this tag then shows the new name. Use the
> old name exactly as `GET api/2.0/files/tags` returns it. Requires a DocSpace admin. Other callers get
> 403. A room admin can create tags but also gets 403 here. The call changes data, and a repeated call
> returns 404. It returns the new name. After the rename, a `tags` filter of `GET api/2.0/files/rooms`
> with the old name finds no rooms. If no tag has the old name, the call returns 404. The call returns
> 400 if a name is empty, has only spaces, or is too long. It also returns 400 if any tag already has
> the new name, even this tag. So you cannot merge two tags.

Before the shortening pass, the text had 133 words. The pass removed 7% (more than the 5% of §4):
`longer than 255 characters` became `too long`, since the schema publishes `maxLength`, and the two
sentences about a taken name merged.
