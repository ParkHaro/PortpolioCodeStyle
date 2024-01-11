using System.Collections;

public class TaskItemCinematic : TaskItem
{
    public string fileName;

    public TaskItemCinematic()
    {
        taskType = TaskType.TaskCinematic;
    }

    public override TaskData TaskItemToData()
    {
        TaskData data = base.TaskItemToData();
        data.value = $"{fileName}";
        return data;
    }

    public override void TaskDataToItem(TaskData taskData)
    {
        base.TaskDataToItem(taskData);
        var dataArray = taskData.value.Split(',');
        this.fileName = dataArray[0];
    }
    
    public override void Copy(TaskItem originItem)
    {
        base.Copy(originItem);
        if (!(originItem is TaskItemCinematic target)) return;
        target.fileName = this.fileName;
    }


    public override IEnumerator CoProcess()
    {
        CinematicManager cinematicManager = CinematicManager.Instance;
        cinematicManager.BeginCinematic(fileName, 0, true);
        yield return cinematicManager.GetCurrentSubCinematicProcess()._cinematicProcessRoutine;
        yield return null;
    }
}