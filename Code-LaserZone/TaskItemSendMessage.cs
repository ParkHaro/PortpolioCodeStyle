using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

public class TaskItemSendMessage : TaskItem
{
    public string className;
    public string funcName;
    public string argument;

    public TaskItemSendMessage(string className, string funcName, string argument)
    {
        taskType = TaskType.TaskSendMessage;
        this.className = className;
        this.funcName = funcName;
        this.argument = argument;
    }

    public override TaskData TaskItemToData()
    {
        TaskData data = base.TaskItemToData();
        data.value = $"{className}," +
                     $"{funcName}," +
                     $"{argument}";
        return data;
    }

    public override void TaskDataToItem(TaskData taskData)
    {
        base.TaskDataToItem(taskData);
        var dataArray = taskData.value.Split(',');

        this.className = dataArray[0];
        this.funcName = dataArray[1];
        this.argument = dataArray[2];
    }

    public override void Copy(TaskItem originItem)
    {
        base.Copy(originItem);
        if (!(originItem is TaskItemSendMessage origin)) return;
        this.className = origin.className;
        this.funcName = origin.funcName;
        this.argument = origin.argument;
    }

    public override void Skip()
    {
        base.Skip();
        if (string.IsNullOrEmpty(funcName))
        {
            Debug.Log("TaskItemSendMessage funcName is NULL");
            return;
        }

        MonoBehaviour target = Object.FindObjectOfType(Type.GetType(className)) as MonoBehaviour;

        if (target != null)
        {
            if (string.IsNullOrEmpty(argument))
            {
                target.SendMessage(funcName);
            }
            else
            {
                if (int.TryParse(argument, out int value))
                {
                    target.SendMessage(funcName, value);
                }
                else
                {
                    target.SendMessage(funcName, argument);
                }
            }
        }
    }

    public override IEnumerator CoProcess()
    {
        yield return null;
        if (string.IsNullOrEmpty(funcName))
        {
            Debug.Log("TaskItemSendMessage funcName is NULL");
            yield break;
        }

        MonoBehaviour target = Object.FindObjectOfType(Type.GetType(className)) as MonoBehaviour;
        
        if (target != null)
        {
            if (string.IsNullOrEmpty(argument))
            {
                target.SendMessage(funcName);
            }
            else
            {
                if (int.TryParse(argument, out int intValue))
                {
                    target.SendMessage(funcName, intValue);
                }
                else if (float.TryParse(argument, out float floatValue))
                {
                    target.SendMessage(funcName, floatValue);
                }
                else
                {
                    target.SendMessage(funcName, argument);
                }
            }
        }
    }
}