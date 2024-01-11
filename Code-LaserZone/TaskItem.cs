using System.Collections;
using UnityEngine;

public class TaskItem
{
    public TaskType taskType = TaskType.None;
    public string description = "Description";
    public bool isWait = true;

    public virtual TaskData TaskItemToData()
    {
        TaskData taskData = new TaskData();
        taskData.isWait = this.isWait;
        taskData.taskType = this.taskType;
        taskData.description = this.description;
        return taskData;
    }

    public virtual void TaskDataToItem(TaskData taskData)
    {
        this.taskType = taskData.taskType;
        this.description = taskData.description;
        this.isWait = taskData.isWait;
    }
    
    public virtual void Copy(TaskItem originItem)
    {
        this.taskType = originItem.taskType;
        this.description = originItem.description;
    }

    public virtual void Skip()
    {
        // TODO SKIP
    }

    public virtual IEnumerator CoProcess()
    {
        yield return null;
    }
}