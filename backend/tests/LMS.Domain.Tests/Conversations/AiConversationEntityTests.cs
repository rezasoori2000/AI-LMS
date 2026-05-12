using LMS.Domain.Conversations;

namespace LMS.Domain.Tests.Conversations;

public class AiConversationEntityTests
{
    [Fact]
    public void Create_SetsStudentId()
    {
        var studentId    = Guid.NewGuid();
        var conversation = AiConversation.Create(studentId);

        Assert.Equal(studentId, conversation.StudentId);
    }

    [Fact]
    public void Create_SetsStartedAt()
    {
        var before       = DateTime.UtcNow.AddSeconds(-1);
        var conversation = AiConversation.Create(Guid.NewGuid());

        Assert.True(conversation.StartedAt >= before);
    }

    [Fact]
    public void Create_ThrowsOnEmptyStudentId()
    {
        Assert.Throws<ArgumentException>(() => AiConversation.Create(Guid.Empty));
    }

    [Fact]
    public void Create_InitialMessagesIsEmpty()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());

        Assert.Empty(conversation.Messages);
    }

    [Fact]
    public void AddMessage_AddsToMessages()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());

        conversation.AddMessage(MessageRole.User, "Hello");

        Assert.Single(conversation.Messages);
    }

    [Fact]
    public void AddMessage_ReturnsMessageWithCorrectRole()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());

        var message = conversation.AddMessage(MessageRole.Assistant, "Hi there!");

        Assert.Equal(MessageRole.Assistant, message.Role);
        Assert.Equal("Hi there!", message.Content);
    }

    [Fact]
    public void AddMessage_SetsConversationId()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());

        var message = conversation.AddMessage(MessageRole.User, "Question?");

        Assert.Equal(conversation.Id, message.ConversationId);
    }

    [Fact]
    public void AddMessage_ThrowsOnEmptyContent()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());

        Assert.Throws<ArgumentException>(() =>
            conversation.AddMessage(MessageRole.User, "  "));
    }

    [Fact]
    public void End_SetsEndedAt()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());

        conversation.End();

        Assert.NotNull(conversation.EndedAt);
    }

    [Fact]
    public void End_IsIdempotent()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());
        conversation.End();
        var firstEndedAt = conversation.EndedAt;

        // Second call must not throw and must not change EndedAt.
        conversation.End();

        Assert.Equal(firstEndedAt, conversation.EndedAt);
    }

    [Fact]
    public void AddMessage_AfterEnd_Throws()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());
        conversation.End();

        Assert.Throws<InvalidOperationException>(() =>
            conversation.AddMessage(MessageRole.User, "Can I still talk?"));
    }

    [Fact]
    public void AddMessage_MultipleMessages_PreservesOrder()
    {
        var conversation = AiConversation.Create(Guid.NewGuid());
        conversation.AddMessage(MessageRole.User,      "First");
        conversation.AddMessage(MessageRole.Assistant, "Second");
        conversation.AddMessage(MessageRole.User,      "Third");

        var messages = conversation.Messages.ToList();
        Assert.Equal("First",  messages[0].Content);
        Assert.Equal("Second", messages[1].Content);
        Assert.Equal("Third",  messages[2].Content);
    }
}
