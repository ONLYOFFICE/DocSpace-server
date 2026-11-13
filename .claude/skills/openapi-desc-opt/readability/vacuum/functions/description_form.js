// vacuum (goja) custom function: form checks of one operation description.
// Options: check = sentenceLength | averageSentence | minSentences | oneParagraph | grade; max / min.
// Counting matches Vale's Docs.SentenceLength (\w+ tokens, so a hyphenated word is two), except that
// code in backticks counts as one word instead of none.
function getSchema() {
    return {
        "name": "descriptionForm",
        "description": "Form checks of an operation description: sentence length, average, sentence count, one paragraph, Flesch-Kincaid grade"
    };
}

function clean(t) {
    return String(t)
        .replace(/`[^`]*`/g, " Code ")
        .replace(/https?:\/\/\S+/g, " URL ")
        .replace(/[*_#>|]/g, " ")
        .replace(/\s+/g, " ")
        .trim();
}
function sentences(t) {
    return t.replace(/([.!?])\s+(?=[A-Z(0-9])/g, "$1\u0000").split("\u0000").filter(function (s) { return /[A-Za-z]/.test(s); });
}
function tokens(t) { return t.match(/\w+/g) || []; }
function letterWords(t) { return t.match(/[A-Za-z][A-Za-z'-]*/g) || []; }
function syllables(w) {
    w = w.toLowerCase();
    if (w.length <= 3) return 1;
    w = w.replace(/(?:[^laeiouy]es|ed|[^laeiouy]e)$/, "").replace(/^y/, "");
    var m = w.match(/[aeiouy]{1,2}/g);
    return Math.max(1, m ? m.length : 0);
}

function runRule(input) {
    var o = context.ruleAction.functionOptions || {};
    if (typeof input !== "string" || !input.trim()) return [];
    var text = clean(input), s = sentences(text);
    switch (o.check) {
        case "sentenceLength":
            var out = [];
            for (var i = 0; i < s.length; i++) {
                var n = tokens(s[i]).length;
                if (n > Number(o.max)) out.push({ message: "sentence " + (i + 1) + " has " + n + " words, more than " + o.max });
            }
            return out;
        case "averageSentence":
            var avg = tokens(text).length / Math.max(1, s.length);
            return avg > Number(o.max) ? [{ message: "average " + avg.toFixed(1) + " words per sentence, more than " + o.max }] : [];
        case "minSentences":
            return s.length < Number(o.min) ? [{ message: s.length + " sentences, fewer than " + o.min }] : [];
        case "oneParagraph":
            // A numbered item starts a list only at the start of the text, or at a line start with the
            // number 1 (CommonMark): a wrapped line that begins with "403. " continues the paragraph.
            // tools.py has_paragraphs holds the same two expressions.
            var raw = String(input);
            return /\n\s*\n|^\s*[-*+] |^\s*1[.)] /m.test(raw) || /^\s*\d{1,9}[.)] /.test(raw)
                ? [{ message: "more than one paragraph or a list" }] : [];
        case "grade":
            var w = letterWords(text), syl = 0;
            for (var j = 0; j < w.length; j++) syl += syllables(w[j]);
            var g = 0.39 * (w.length / Math.max(1, s.length)) + 11.8 * (syl / Math.max(1, w.length)) - 15.59;
            return g > Number(o.max) ? [{ message: "Flesch-Kincaid grade " + g.toFixed(1) + ", more than " + o.max }] : [];
    }
    return [{ message: "unknown check " + o.check }];
}
