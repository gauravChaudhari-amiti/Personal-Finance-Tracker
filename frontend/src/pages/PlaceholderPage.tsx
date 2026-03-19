type Props = {
  title: string;
  description: string;
};

export default function PlaceholderPage({ title, description }: Props) {
  return (
    <div className="card">
      <h1 className="page-title">{title}</h1>
      <p className="meta-text">{description}</p>
      <p>This section is ready for the next module code.</p>
    </div>
  );
}
