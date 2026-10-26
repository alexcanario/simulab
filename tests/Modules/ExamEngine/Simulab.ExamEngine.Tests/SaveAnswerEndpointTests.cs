using System.Net;
using Simulab.ExamEngine.Contracts;

namespace Simulab.ExamEngine.Tests;

/// <summary>
/// F-166 UC2 through HTTP: saving, replacing and clearing the answer to one question (BR4, BR5, BR9), with the shape of
/// each of the eight types checked against the question's version (T5) and the size limits (T6).
/// </summary>
public sealed class SaveAnswerEndpointTests : ExamEngineApiTests
{
    private static AnswerPayload ValidAnswer(AttemptQuestionType type) => type switch
    {
        AttemptQuestionType.SingleChoice => new SingleChoicePayload(2),
        AttemptQuestionType.TrueFalse => new TrueFalsePayload(false),
        AttemptQuestionType.MultipleAnswer => new MultipleAnswerPayload([0, 1]),
        AttemptQuestionType.Matching => new MatchingPayload([new(0, 2), new(1, 0)]),
        AttemptQuestionType.FillInTheBlanks => new FillInTheBlanksPayload([new(1, "Brasília"), new(2, "1960")]),
        AttemptQuestionType.ShortAnswer => new ShortAnswerPayload("Brasília"),
        AttemptQuestionType.OpenAnswer => new OpenAnswerPayload(new string('a', 50)),
        AttemptQuestionType.Essay => new EssayPayload("primeira\nsegunda\nterceira"),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    private async Task<(HttpClient Student, AttemptResponse Attempt, IReadOnlyList<PaperQuestion> Questions)> StartedEveryTypeAsync()
    {
        var admin = await AdminAsync();
        var (edition, questions) = await PublishedEditionWithEveryTypeAsync(admin);
        var student = await StudentAsync();

        return (student, await StartedAsync(student, edition.Id), questions);
    }

    private static Guid QuestionOf(IReadOnlyList<PaperQuestion> questions, AttemptQuestionType type) =>
        questions.Single(question => question.Type == type).Id;

    private static async Task AssertEverythingBlankAsync(HttpClient student, Guid attemptId)
    {
        (await ReadAsync(student, attemptId)).Questions.Should().OnlyContain(question => question.Answer == null);
    }

    // AC7
    [Fact]
    public async Task Save_AValidAnswerOfEachOfTheEightTypes_IsReadBackAsSaved()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();

        foreach (var type in Enum.GetValues<AttemptQuestionType>())
        {
            await SavedAsync(student, attempt.Id, QuestionOf(questions, type), ValidAnswer(type));
        }

        var reread = await ReadAsync(student, attempt.Id);
        foreach (var question in reread.Questions)
        {
            Json(question.Answer).Should().Be(Json<AnswerPayload?>(ValidAnswer(question.Type)), question.Type.ToString());
        }
    }

    // AC7: a save answers with the question as the attempt shows it.
    [Fact]
    public async Task Save_Answers200WithTheQuestionAndItsAnswer()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var questionId = QuestionOf(questions, AttemptQuestionType.MultipleAnswer);

        var response = await SaveAsync(student, attempt.Id, questionId, ValidAnswer(AttemptQuestionType.MultipleAnswer));

        var body = await BodyAsync(response, HttpStatusCode.OK);
        var saved = System.Text.Json.JsonSerializer.Deserialize<AttemptQuestionResponse>(body, Simulab.SharedKernel.Serialization.AppJson.Options)!;
        saved.QuestionId.Should().Be(questionId);
        saved.Type.Should().Be(AttemptQuestionType.MultipleAnswer);
        Json(saved.Answer).Should().Be(Json<AnswerPayload?>(ValidAnswer(AttemptQuestionType.MultipleAnswer)));
    }

    // AC7: typed text is kept as written, with the line breaks of a Windows keyboard made one.
    [Fact]
    public async Task Save_AnEssayWithWindowsLineBreaks_IsKeptWithOneKindOfLineBreakAndNothingElseChanged()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var essay = QuestionOf(questions, AttemptQuestionType.Essay);

        await SavedAsync(student, attempt.Id, essay, new EssayPayload("  um\r\ndois\rtrês  "));

        (await ReadAsync(student, attempt.Id)).Questions.Single(question => question.QuestionId == essay).Answer
            .Should().Be(new EssayPayload("  um\ndois\ntrês  "));
    }

    // AC8: [sec] T5. The body names something the question's version does not have.
    [Theory]
    [InlineData(AttemptQuestionType.SingleChoice, "single-unknown-option")]
    [InlineData(AttemptQuestionType.SingleChoice, "single-no-option")]
    [InlineData(AttemptQuestionType.TrueFalse, "true-false-no-value")]
    [InlineData(AttemptQuestionType.MultipleAnswer, "multiple-no-option")]
    [InlineData(AttemptQuestionType.MultipleAnswer, "multiple-unknown-option")]
    [InlineData(AttemptQuestionType.Matching, "matching-unknown-item")]
    [InlineData(AttemptQuestionType.Matching, "matching-unknown-option")]
    [InlineData(AttemptQuestionType.FillInTheBlanks, "blanks-unknown-number")]
    [InlineData(AttemptQuestionType.ShortAnswer, "short-above-the-cap")]
    [InlineData(AttemptQuestionType.OpenAnswer, "open-above-the-question-limit")]
    [InlineData(AttemptQuestionType.Essay, "essay-above-the-question-limit")]
    [InlineData(AttemptQuestionType.Essay, "essay-above-the-cap")]
    [InlineData(AttemptQuestionType.SingleChoice, "another-type")]
    public async Task Save_AnAnswerThatDoesNotFitTheQuestion_Answers400WithOneEntryPerField_AndStoresNothing(
        AttemptQuestionType type, string scenario)
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var (answer, field, code) = scenario switch
        {
            "single-unknown-option" => ((AnswerPayload)new SingleChoicePayload(7), "optionPosition", ExamAttemptErrorCodes.OptionUnknown),
            "single-no-option" => (new SingleChoicePayload(), "optionPosition", ExamAttemptErrorCodes.AnswerEmpty),
            "true-false-no-value" => (new TrueFalsePayload(), "value", ExamAttemptErrorCodes.AnswerEmpty),
            "multiple-no-option" => (new MultipleAnswerPayload([]), "optionPositions", ExamAttemptErrorCodes.AnswerEmpty),
            "multiple-unknown-option" => (new MultipleAnswerPayload([0, 9]), "optionPositions[1]", ExamAttemptErrorCodes.OptionUnknown),
            "matching-unknown-item" => (new MatchingPayload([new(9, 0)]), "pairs[0].itemPosition", ExamAttemptErrorCodes.ItemUnknown),
            "matching-unknown-option" => (new MatchingPayload([new(0, 9)]), "pairs[0].optionPosition", ExamAttemptErrorCodes.OptionUnknown),
            "blanks-unknown-number" => (new FillInTheBlanksPayload([new(9, "x")]), "blanks[0].number", ExamAttemptErrorCodes.BlankUnknown),
            "short-above-the-cap" => (new ShortAnswerPayload(new string('a', AttemptLimits.TextMaxLength + 1)), "text", ExamAttemptErrorCodes.TextTooLong),
            "open-above-the-question-limit" => (new OpenAnswerPayload(new string('a', 51)), "text", ExamAttemptErrorCodes.TextTooLong),
            "essay-above-the-question-limit" => (new EssayPayload("1\n2\n3\n4\n5\n6"), "text", ExamAttemptErrorCodes.TextTooLong),
            "essay-above-the-cap" => (new EssayPayload(new string('a', AttemptLimits.TextMaxLength + 1)), "text", ExamAttemptErrorCodes.TextTooLong),
            "another-type" => (new TrueFalsePayload(true), "type", ExamAttemptErrorCodes.AnswerTypeMismatch),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var response = await SaveAsync(student, attempt.Id, QuestionOf(questions, type), answer);

        var body = await BodyAsync(response, HttpStatusCode.BadRequest);
        CodeOf(body).Should().Be(ExamAttemptErrorCodes.AnswerInvalid);
        ErrorsOf(body).Should().Contain((field, code));
        await AssertEverythingBlankAsync(student, attempt.Id);
    }

    // AC8: [sec] T5. A body with no type, or a type nobody knows, is the same refusal with a code, not a framework error.
    [Theory]
    [InlineData("""{"optionPosition":1}""")]
    [InlineData("""{"type":"Banana","optionPosition":1}""")]
    [InlineData("""{"type":null}""")]
    public async Task Save_ABodyWithAMissingOrUnknownType_Answers400AnswerInvalid_AndStoresNothing(string json)
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();

        var response = await SaveRawAsync(student, attempt.Id, QuestionOf(questions, AttemptQuestionType.SingleChoice), json);

        var body = await BodyAsync(response, HttpStatusCode.BadRequest);
        CodeOf(body).Should().Be(ExamAttemptErrorCodes.AnswerInvalid);
        ErrorsOf(body).Should().Contain(("type", ExamAttemptErrorCodes.AnswerTypeMismatch));
        await AssertEverythingBlankAsync(student, attempt.Id);
    }

    // AC8: [sec] T5. A question that is not one of the attempt, whatever its edition.
    [Fact]
    public async Task Save_AQuestionThatIsNotInTheAttempt_Answers404_AndStoresNothing()
    {
        var admin = await AdminAsync();
        var topicId = await TopicAsync(admin);
        var (edition, _) = await PublishedEditionWithEveryTypeAsync(admin);
        var otherEdition = await EditionAsync(admin, publish: false);
        var foreign = await PublishedQuestionAsync(admin, topicId, otherEdition.Id, 1, AttemptQuestionType.SingleChoice);
        var student = await StudentAsync();
        var attempt = await StartedAsync(student, edition.Id);

        foreach (var questionId in new[] { foreign.Id, Guid.CreateVersion7() })
        {
            var response = await SaveAsync(student, attempt.Id, questionId, new SingleChoicePayload(0));

            CodeOf(await BodyAsync(response, HttpStatusCode.NotFound)).Should().Be(ExamAttemptErrorCodes.QuestionNotInAttempt);
            (await ClearAsync(student, attempt.Id, questionId)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        await AssertEverythingBlankAsync(student, attempt.Id);
    }

    // AC9
    [Fact]
    public async Task Save_AnotherAnswerToTheSameQuestion_ReplacesTheFirst_AndTheLastOfTwoInARowIsWhatIsRead()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var questionId = QuestionOf(questions, AttemptQuestionType.SingleChoice);

        await SavedAsync(student, attempt.Id, questionId, new SingleChoicePayload(0));
        await SavedAsync(student, attempt.Id, questionId, new SingleChoicePayload(1));
        await SavedAsync(student, attempt.Id, questionId, new SingleChoicePayload(2));

        (await ReadAsync(student, attempt.Id)).Questions.Single(question => question.QuestionId == questionId).Answer
            .Should().Be(new SingleChoicePayload(2));
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempt_answers WHERE attempt_id = '{attempt.Id}'")).Should().Be(8);
    }

    // AC9: clearing makes the question blank again, whatever its state; clearing a blank one is no failure.
    [Fact]
    public async Task Clear_MakesTheQuestionBlankAgain_AndClearingABlankOneAnswers204Too()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var questionId = QuestionOf(questions, AttemptQuestionType.TrueFalse);
        await SavedAsync(student, attempt.Id, questionId, new TrueFalsePayload(true));

        (await ClearAsync(student, attempt.Id, questionId)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ClearAsync(student, attempt.Id, questionId)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        await AssertEverythingBlankAsync(student, attempt.Id);
    }

    // AC9: [sec] BR9 two saves at once: one of them is what is read, and one row stays.
    [Fact]
    public async Task Save_TwoAtTheSameTime_LeavesOneOfThemAndOneRow()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var questionId = QuestionOf(questions, AttemptQuestionType.SingleChoice);

        var responses = await Task.WhenAll(
            SaveAsync(student, attempt.Id, questionId, new SingleChoicePayload(0)),
            SaveAsync(student, attempt.Id, questionId, new SingleChoicePayload(1)));

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK);
        (await ReadAsync(student, attempt.Id)).Questions.Single(question => question.QuestionId == questionId).Answer
            .Should().BeOneOf(new SingleChoicePayload(0), new SingleChoicePayload(1));
        (await CountAsync($"SELECT count(*) FROM exam_engine.attempt_answers WHERE attempt_id = '{attempt.Id}' AND question_id = '{questionId}'")).Should().Be(1);
    }

    // AC17: [sec] T6. A body above the limit of the route is refused whole.
    [Fact]
    public async Task Save_ABodyAboveTheLimitOfTheRoute_Answers4xx_AndStoresNothing()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var json = $$"""{"type":"ShortAnswer","text":"{{new string('a', AttemptLimits.SaveBodyMaxBytes + 1)}}"}""";

        var response = await SaveRawAsync(student, attempt.Id, QuestionOf(questions, AttemptQuestionType.ShortAnswer), json);

        ((int)response.StatusCode).Should().BeInRange(400, 499);
        await AssertEverythingBlankAsync(student, attempt.Id);
    }

    // AC17: the limit leaves room for the largest valid text, every character written as a six-byte escape.
    [Fact]
    public async Task Save_ATextOfTheLargestSizeWithAccents_IsAccepted()
    {
        var (student, attempt, questions) = await StartedEveryTypeAsync();
        var questionId = QuestionOf(questions, AttemptQuestionType.ShortAnswer);
        var text = new string('é', AttemptLimits.TextMaxLength);

        await SavedAsync(student, attempt.Id, questionId, new ShortAnswerPayload(text));

        (await ReadAsync(student, attempt.Id)).Questions.Single(question => question.QuestionId == questionId).Answer
            .Should().Be(new ShortAnswerPayload(text));
    }
}
