export default function ProgressIndicator({ currentStep, steps = ['Address', 'Payment', 'Confirmation'] }) {
  return (
    <div className="progress-indicator">
      <div className="progress-container">
        {steps.map((step, index) => (
          <div key={step} className={`progress-step ${index < currentStep ? 'completed' : ''} ${index === currentStep ? 'active' : ''}`}>
            <div className="step-number">
              {index < currentStep ? '✓' : index + 1}
            </div>
            <div className="step-label">{step}</div>
            {index < steps.length - 1 && (
              <div className={`step-line ${index < currentStep ? 'completed' : ''}`} />
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
