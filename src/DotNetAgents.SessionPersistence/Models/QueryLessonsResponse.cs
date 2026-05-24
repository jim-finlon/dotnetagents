// SPDX-License-Identifier: Apache-2.0

namespace DotNetAgents.SessionPersistence.Models;

public record QueryLessonsResponse(
    IReadOnlyList<LessonInfo> Lessons,
    int TotalCount
);
