using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ForumApi.Data;

namespace ForumApi.Functions;

public class TopicArchiveTimer
{
    private readonly ILogger _logger;
    private readonly ForumContext _context;
    private const int DefaultArchiveTimeRangeInDays = 30;

    public TopicArchiveTimer(ILoggerFactory loggerFactory, ForumContext context)
    {
        _logger = loggerFactory.CreateLogger<TopicArchiveTimer>();
        _context = context;
    }

    [Function("TopicArchiveTimer")]
    public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo? myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        
        if (myTimer?.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }
        var timeRangeInDays = Environment.GetEnvironmentVariable("TopicArchiveTimeRangeInDays");
        if (!int.TryParse(timeRangeInDays, out int days))
        {
            days = DefaultArchiveTimeRangeInDays;
        }
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var topicsToArchive = await _context.Topics.Where(t => !t.IsArchived).Include(t => t.Messages).Where(t => t.Messages.Any() ? t.Messages.Max(m => m.CreatedAt) < cutoffDate : t.CreatedAt < cutoffDate).ToListAsync();
        foreach (var topic in topicsToArchive)
        {
            topic.IsArchived = true;
            _logger.LogInformation("Archiving topic with ID: {topicId} and Title: {topicTitle}", topic.Id, topic.Title);
        }
        await _context.SaveChangesAsync();
    }
}