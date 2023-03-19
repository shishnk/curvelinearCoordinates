namespace Project;

public class Newton
{
    private Vector<double> _vector;
    private Vector<double> _result;
    private readonly Point2D _primaryPoint;
    private readonly Vector<double> _slaeResult;
    private readonly Matrix _jacobiMatrix;
    private readonly IBasis _basis;
    private readonly IBaseMesh _mesh;
    private readonly int _ielem;

    public Point2D Result => (_result[0], _result[1]);

    public Newton(IBasis basis, IBaseMesh mesh, Point2D primaryPoint, int ielem)
    {
        _mesh = mesh;
        _basis = basis;
        _primaryPoint = primaryPoint;
        _ielem = ielem;
        _jacobiMatrix = new(2);
        _slaeResult = new(2);
        _vector = new(2);
        _result = new(2);
    }

    public void Compute()
    {
        const int maxIters = 1000;
        const double eps = 1E-12;

        CalculateEquationsValues();

        var primaryNorm = _vector.Norm();
        var currentNorm = primaryNorm;

        for (int iter = 0; iter < maxIters && currentNorm / primaryNorm >= eps; iter++)
        {
            var previousNorm = _vector.Norm();

            var beta = 1.0;

            CalculateJacobiMatrix();

            _vector = -_vector;

            GaussMethod();

            var temp = Vector<double>.Copy(_result);

            do
            {
                _result = temp + beta * _slaeResult;

                CalculateEquationsValues();
                currentNorm = _vector.Norm();

                if (currentNorm > previousNorm) beta /= 2.0;
                else break;
            } while (beta > eps);
        }
    }

    private void CalculateEquationsValues()
    {
        _vector.Fill(0.0);

        var element = _mesh.Elements[_ielem];
        Point2D point = (_result[0], _result[1]);

        for (int i = 0; i < _basis.Size; i++)
        {
            _vector[0] += _basis.GetPsi(i, point) * _mesh.Points[element.Nodes[i]].X;
            _vector[1] += _basis.GetPsi(i, point) * _mesh.Points[element.Nodes[i]].Y;
        }

        _vector[0] -= _primaryPoint.X;
        _vector[1] -= _primaryPoint.Y;
    }

    private void CalculateJacobiMatrix()
    {
        var element = _mesh.Elements[_ielem];

        Span<double> dx = stackalloc double[2];
        Span<double> dy = stackalloc double[2];

        for (int i = 0; i < _basis.Size; i++)
        {
            for (int k = 0; k < _jacobiMatrix.Size; k++)
            {
                dx[k] += _basis.GetDPsi(i, k, _primaryPoint) * _mesh.Points[element.Nodes[i]].X;
                dy[k] += _basis.GetDPsi(i, k, _primaryPoint) * _mesh.Points[element.Nodes[i]].Y;
            }
        }

        _jacobiMatrix[0, 0] = dx[0];
        _jacobiMatrix[0, 1] = dy[0];
        _jacobiMatrix[1, 0] = dx[1];
        _jacobiMatrix[1, 1] = dy[1];
    }

    private void GaussMethod()
    {
        const double eps = 1E-14;

        for (int k = 0; k < 2; k++)
        {
            var max = Math.Abs(_jacobiMatrix[k, k]);
            int index = k;

            for (int i = k + 1; i < 2; i++)
            {
                if (Math.Abs(_jacobiMatrix[i, k]) > max)
                {
                    max = Math.Abs(_jacobiMatrix[i, k]);
                    index = i;
                }
            }

            for (int j = 0; j < 2; j++)
            {
                (_jacobiMatrix[k, j], _jacobiMatrix[index, j]) =
                    (_jacobiMatrix[index, j], _jacobiMatrix[k, j]);
            }

            (_vector[k], _vector[index]) = (_vector[index], _vector[k]);

            for (int i = k; i < 2; i++)
            {
                var temp = _jacobiMatrix[i, k];

                if (Math.Abs(temp) < eps) throw new Exception("Zero element of the column");

                for (int j = 0; j < 2; j++)
                {
                    _jacobiMatrix[i, j] /= temp;
                }

                _vector[i] /= temp;

                if (i == k) continue;
                {
                    for (int j = 0; j < 2; j++)
                        _jacobiMatrix[i, j] -= _jacobiMatrix[k, j];

                    _vector[i] -= _vector[k];
                }
            }
        }

        for (int k = 2 - 1; k >= 0; k--)
        {
            _slaeResult[k] = _vector[k];

            for (int i = 0; i < k; i++)
            {
                _vector[i] -= _jacobiMatrix[i, k] * _slaeResult[k];
            }
        }
    }
}