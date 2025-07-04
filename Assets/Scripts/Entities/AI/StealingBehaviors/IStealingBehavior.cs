using UnityEngine;
public interface IStealingBehavior
{
    void Initialize(CrawlingEntity entity);
    void ExecuteBehavior();
    bool CanExecute();
    void OnBehaviorComplete();
    void OnBehaviorFailed();
    bool IsActive { get; }
    bool IsComplete { get; }
    string BehaviorName { get; }
    void Reset();
    void Stop();
}