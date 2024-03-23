namespace DatasetEditor.model;

public class DatasetImageLabel
{
    public double angle;  // it's an enum number (-3, -2, -1, 0, 1, 2, 3)
    public double speed;  // it's an enum number (1, 2, 3, 4)

    public DatasetImageLabel(int angle, int speed) {
        this.angle = angle;
        this.speed = speed;
    }
}