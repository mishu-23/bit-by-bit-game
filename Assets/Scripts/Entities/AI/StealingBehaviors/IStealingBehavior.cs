using UnityEngine;

public interface IStealingBehavior
{
    #region Behavior Lifecycle
    
    /// <summary>
    /// Initialize the behavior with the entity that will use it
    /// </summary>
    /// <param name="entity">The CrawlingEntity that owns this behavior</param>
    void Initialize(CrawlingEntity entity);
    
    /// <summary>
    /// Execute the main behavior logic
    /// </summary>
    void ExecuteBehavior();
    
    /// <summary>
    /// Check if the behavior can be executed in the current state
    /// </summary>
    /// <returns>True if the behavior can execute</returns>
    bool CanExecute();
    
    /// <summary>
    /// Called when the behavior completes successfully
    /// </summary>
    void OnBehaviorComplete();
    
    /// <summary>
    /// Called when the behavior fails or is interrupted
    /// </summary>
    void OnBehaviorFailed();
    
    #endregion

    #region State Queries
    
    /// <summary>
    /// Check if the behavior is currently active
    /// </summary>
    bool IsActive { get; }
    
    /// <summary>
    /// Check if the behavior has completed (successfully or failed)
    /// </summary>
    bool IsComplete { get; }
    
    /// <summary>
    /// Get the name/type of this behavior for debugging
    /// </summary>
    string BehaviorName { get; }
    
    #endregion

    #region Control
    
    /// <summary>
    /// Reset the behavior to its initial state
    /// </summary>
    void Reset();
    
    /// <summary>
    /// Stop the behavior immediately
    /// </summary>
    void Stop();
    
    #endregion
} 